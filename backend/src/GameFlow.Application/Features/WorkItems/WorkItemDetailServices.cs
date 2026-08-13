using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Notifications.Dtos;
using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Application.Features.WorkItems.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.WorkItems;

/// <summary>
/// Görev detayındaki alt kaynaklar için ortak erişim denetimi.
/// Görüntüleme proje üyeliği, değişiklik ise ilgili kuralla korunur.
/// </summary>
public abstract class WorkItemSubResourceService(
    IApplicationDbContext context,
    IPermissionService permissions)
{
    protected IApplicationDbContext Context { get; } = context;

    protected IPermissionService Permissions { get; } = permissions;

    /// <summary>Görevi bulur ve kullanıcının projeye üyeliğini doğrular.</summary>
    protected async Task<WorkItem> GetAccessibleWorkItemAsync(
        Guid workItemId,
        CancellationToken cancellationToken)
    {
        var workItem = await Context.WorkItems
            .FirstOrDefaultAsync(w => w.Id == workItemId, cancellationToken)
            ?? throw new NotFoundException("Görev", workItemId);

        await Permissions.EnsureProjectMemberAsync(workItem.ProjectId, cancellationToken);

        return workItem;
    }

    /// <summary>Görev üzerinde yönetim yetkisi (proje yöneticisi veya takım lideri).</summary>
    protected async Task<bool> CanManageWorkItemAsync(
        WorkItem workItem,
        CancellationToken cancellationToken)
    {
        if (await Permissions.CanManageProjectAsync(workItem.ProjectId, cancellationToken))
        {
            return true;
        }

        return workItem.TeamId.HasValue
               && await Permissions.CanManageTeamAsync(workItem.TeamId.Value, cancellationToken);
    }

    protected static readonly System.Linq.Expressions.Expression<Func<User, UserSummaryDto>>
        UserSummary = user => new UserSummaryDto(
            user.Id,
            user.FullName,
            user.Email,
            user.JobTitle,
            user.AvatarUrl,
            (SystemRole)user.RoleId,
            user.IsOnline,
            user.LastSeenAt);
}

/// <summary>Görev kontrol listesi maddeleri.</summary>
public class WorkItemChecklistService(
    IApplicationDbContext context,
    IPermissionService permissions,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTime) : WorkItemSubResourceService(context, permissions), IWorkItemChecklistService
{
    public async Task<IReadOnlyList<ChecklistItemDto>> AddAsync(
        Guid workItemId,
        CreateChecklistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var workItem = await GetAccessibleWorkItemAsync(workItemId, cancellationToken);

        await EnsureCanModifyAsync(workItem, cancellationToken);

        var nextOrder = await Context.TaskChecklistItems
            .Where(i => i.WorkItemId == workItemId)
            .MaxAsync(i => (int?)i.Order, cancellationToken) ?? -1;

        Context.TaskChecklistItems.Add(new TaskChecklistItem
        {
            WorkItemId = workItemId,
            Text = request.Text.Trim(),
            Order = nextOrder + 1
        });

        await Context.SaveChangesAsync(cancellationToken);

        return await GetListAsync(workItemId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistItemDto>> UpdateAsync(
        Guid workItemId,
        Guid itemId,
        UpdateChecklistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var workItem = await GetAccessibleWorkItemAsync(workItemId, cancellationToken);

        await EnsureCanModifyAsync(workItem, cancellationToken);

        var item = await Context.TaskChecklistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WorkItemId == workItemId, cancellationToken)
            ?? throw new NotFoundException("Kontrol listesi maddesi", itemId);

        item.Text = request.Text.Trim();

        // Tamamlanma bilgisi yalnızca durum değiştiğinde güncellenir.
        if (item.IsCompleted != request.IsCompleted)
        {
            item.IsCompleted = request.IsCompleted;
            item.CompletedAt = request.IsCompleted ? dateTime.UtcNow : null;
            item.CompletedById = request.IsCompleted ? currentUser.UserId : null;
        }

        await Context.SaveChangesAsync(cancellationToken);

        return await GetListAsync(workItemId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistItemDto>> DeleteAsync(
        Guid workItemId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var workItem = await GetAccessibleWorkItemAsync(workItemId, cancellationToken);

        await EnsureCanModifyAsync(workItem, cancellationToken);

        var item = await Context.TaskChecklistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WorkItemId == workItemId, cancellationToken)
            ?? throw new NotFoundException("Kontrol listesi maddesi", itemId);

        Context.TaskChecklistItems.Remove(item);

        await Context.SaveChangesAsync(cancellationToken);

        return await GetListAsync(workItemId, cancellationToken);
    }

    private async Task<IReadOnlyList<ChecklistItemDto>> GetListAsync(
        Guid workItemId,
        CancellationToken cancellationToken)
        => await Context.TaskChecklistItems
            .AsNoTracking()
            .Where(i => i.WorkItemId == workItemId)
            .OrderBy(i => i.Order)
            .Select(i => new ChecklistItemDto(i.Id, i.Text, i.IsCompleted, i.Order, i.CompletedAt))
            .ToListAsync(cancellationToken);

    /// <summary>Kontrol listesini atanan kişi, görevi açan kişi veya yöneticiler değiştirebilir.</summary>
    private async Task EnsureCanModifyAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        if (workItem.AssigneeId == currentUser.UserId || workItem.ReporterId == currentUser.UserId)
        {
            return;
        }

        if (await CanManageWorkItemAsync(workItem, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException("Kontrol listesini değiştirme yetkiniz bulunmuyor.");
    }
}

/// <summary>Görev yorumları.</summary>
public class WorkItemCommentService(
    IApplicationDbContext context,
    IPermissionService permissions,
    ICurrentUserService currentUser,
    INotificationService notifications,
    IActivityLogger activityLogger,
    IDateTimeProvider dateTime) : WorkItemSubResourceService(context, permissions), IWorkItemCommentService
{
    public async Task<CommentDto> AddAsync(
        Guid workItemId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var workItem = await GetAccessibleWorkItemAsync(workItemId, cancellationToken);
        var authorId = currentUser.RequireUserId();

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await Context.TaskComments.AnyAsync(
                c => c.Id == request.ParentCommentId.Value && c.WorkItemId == workItemId,
                cancellationToken);

            if (!parentExists)
            {
                throw new NotFoundException("Yorum", request.ParentCommentId.Value);
            }
        }

        var comment = new TaskComment
        {
            WorkItemId = workItemId,
            AuthorId = authorId,
            Content = request.Content.Trim(),
            ParentCommentId = request.ParentCommentId
        };

        Context.TaskComments.Add(comment);

        activityLogger.Log(
            ActivityType.TaskCommented,
            $"{workItem.Key} görevine yorum yapıldı.",
            projectId: workItem.ProjectId,
            teamId: workItem.TeamId,
            workItemId: workItem.Id,
            entityType: nameof(TaskComment),
            entityId: comment.Id);

        // Atanan kişi, görevi açan kişi ve varsa yanıtlanan yorumun sahibi bilgilendirilir.
        var recipients = new HashSet<Guid>();

        if (workItem.AssigneeId.HasValue)
        {
            recipients.Add(workItem.AssigneeId.Value);
        }

        if (workItem.ReporterId.HasValue)
        {
            recipients.Add(workItem.ReporterId.Value);
        }

        if (request.ParentCommentId.HasValue)
        {
            var parentAuthorId = await Context.TaskComments
                .Where(c => c.Id == request.ParentCommentId.Value)
                .Select(c => c.AuthorId)
                .FirstOrDefaultAsync(cancellationToken);

            if (parentAuthorId != Guid.Empty)
            {
                recipients.Add(parentAuthorId);
            }
        }

        notifications.QueueMany(recipients.Select(recipientId => new NotificationRequest(
            recipientId,
            NotificationType.TaskCommented,
            "Yeni yorum",
            $"{workItem.Key} · {workItem.Title}",
            $"/gorevler/{workItem.Key}",
            nameof(WorkItem),
            workItem.Id)));

        await Context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetCommentAsync(comment.Id, cancellationToken);
    }

    public async Task<CommentDto> UpdateAsync(
        Guid workItemId,
        Guid commentId,
        UpdateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        await GetAccessibleWorkItemAsync(workItemId, cancellationToken);

        var comment = await Context.TaskComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.WorkItemId == workItemId, cancellationToken)
            ?? throw new NotFoundException("Yorum", commentId);

        // Yorumu yalnızca sahibi düzenleyebilir.
        if (comment.AuthorId != currentUser.UserId)
        {
            throw new ForbiddenException("Yalnızca kendi yorumunuzu düzenleyebilirsiniz.");
        }

        comment.Content = request.Content.Trim();
        comment.IsEdited = true;
        comment.EditedAt = dateTime.UtcNow;

        await Context.SaveChangesAsync(cancellationToken);

        return await GetCommentAsync(comment.Id, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid workItemId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var workItem = await GetAccessibleWorkItemAsync(workItemId, cancellationToken);

        var comment = await Context.TaskComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.WorkItemId == workItemId, cancellationToken)
            ?? throw new NotFoundException("Yorum", commentId);

        // Sahibi veya yöneticiler silebilir (moderasyon).
        if (comment.AuthorId != currentUser.UserId
            && !await CanManageWorkItemAsync(workItem, cancellationToken))
        {
            throw new ForbiddenException("Bu yorumu silme yetkiniz bulunmuyor.");
        }

        Context.TaskComments.Remove(comment);

        await Context.SaveChangesAsync(cancellationToken);
    }

    private async Task<CommentDto> GetCommentAsync(Guid commentId, CancellationToken cancellationToken)
        => await Context.TaskComments
            .AsNoTracking()
            .Where(c => c.Id == commentId)
            .Select(c => new CommentDto(
                c.Id,
                c.Content,
                new UserSummaryDto(
                    c.Author.Id,
                    c.Author.FullName,
                    c.Author.Email,
                    c.Author.JobTitle,
                    c.Author.AvatarUrl,
                    (SystemRole)c.Author.RoleId,
                    c.Author.IsOnline,
                    c.Author.LastSeenAt),
                c.CreatedAt,
                c.IsEdited,
                c.EditedAt,
                c.ParentCommentId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Yorum", commentId);
}

/// <summary>Görev dosya ekleri.</summary>
public class WorkItemAttachmentService(
    IApplicationDbContext context,
    IPermissionService permissions,
    ICurrentUserService currentUser,
    IFileStorageService fileStorage,
    IActivityLogger activityLogger)
    : WorkItemSubResourceService(context, permissions), IWorkItemAttachmentService
{
    public async Task<AttachmentDto> UploadAsync(
        Guid workItemId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var workItem = await GetAccessibleWorkItemAsync(workItemId, cancellationToken);

        var stored = await fileStorage.SaveAsync(
            content,
            fileName,
            contentType,
            // Dosyalar proje bazlı klasörlenir; böylece disk üzerinde takip edilebilir kalır.
            $"gorevler/{workItem.ProjectId:N}",
            cancellationToken);

        var attachment = new TaskAttachment
        {
            WorkItemId = workItemId,
            UploadedById = currentUser.UserId,
            FileName = stored.FileName,
            StoredFileName = stored.StoredFileName,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            Category = stored.Category,
            Url = stored.Url
        };

        Context.TaskAttachments.Add(attachment);

        activityLogger.Log(
            ActivityType.AttachmentUploaded,
            $"{workItem.Key} görevine \"{stored.FileName}\" dosyası eklendi.",
            projectId: workItem.ProjectId,
            teamId: workItem.TeamId,
            workItemId: workItem.Id,
            entityType: nameof(TaskAttachment),
            entityId: attachment.Id);

        await Context.SaveChangesAsync(cancellationToken);

        return await GetAttachmentAsync(attachment.Id, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid workItemId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var workItem = await GetAccessibleWorkItemAsync(workItemId, cancellationToken);

        var attachment = await Context.TaskAttachments
            .FirstOrDefaultAsync(
                a => a.Id == attachmentId && a.WorkItemId == workItemId,
                cancellationToken)
            ?? throw new NotFoundException("Dosya", attachmentId);

        if (attachment.UploadedById != currentUser.UserId
            && !await CanManageWorkItemAsync(workItem, cancellationToken))
        {
            throw new ForbiddenException("Bu dosyayı silme yetkiniz bulunmuyor.");
        }

        Context.TaskAttachments.Remove(attachment);

        activityLogger.Log(
            ActivityType.AttachmentDeleted,
            $"{workItem.Key} görevinden \"{attachment.FileName}\" dosyası silindi.",
            projectId: workItem.ProjectId,
            workItemId: workItem.Id,
            entityType: nameof(TaskAttachment),
            entityId: attachment.Id);

        await Context.SaveChangesAsync(cancellationToken);

        // Veritabanı kaydı silindikten sonra fiziksel dosya kaldırılır; ters sırada
        // yapılsaydı kayıt silinemezse dosya kaybolurdu.
        await fileStorage.DeleteAsync(
            attachment.StoredFileName,
            $"gorevler/{workItem.ProjectId:N}",
            cancellationToken);
    }

    private async Task<AttachmentDto> GetAttachmentAsync(Guid id, CancellationToken cancellationToken)
        => await Context.TaskAttachments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                a.Category,
                a.Url,
                a.UploadedBy == null
                    ? null
                    : new UserSummaryDto(
                        a.UploadedBy.Id,
                        a.UploadedBy.FullName,
                        a.UploadedBy.Email,
                        a.UploadedBy.JobTitle,
                        a.UploadedBy.AvatarUrl,
                        (SystemRole)a.UploadedBy.RoleId,
                        a.UploadedBy.IsOnline,
                        a.UploadedBy.LastSeenAt),
                a.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Dosya", id);
}

/// <summary>Proje bazlı görev etiketleri.</summary>
public class LabelService(
    IApplicationDbContext context,
    IPermissionService permissions) : ILabelService
{
    public async Task<IReadOnlyList<LabelDto>> GetListAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureProjectMemberAsync(projectId, cancellationToken);

        return await context.Labels
            .AsNoTracking()
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Name)
            .Select(l => new LabelDto(l.Id, l.Name, l.ColorHex))
            .ToListAsync(cancellationToken);
    }

    public async Task<LabelDto> CreateAsync(
        Guid projectId,
        CreateLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureProjectMemberAsync(projectId, cancellationToken);

        var name = request.Name.Trim();

        var exists = await context.Labels
            .AnyAsync(l => l.ProjectId == projectId && l.Name == name, cancellationToken);

        if (exists)
        {
            throw new ConflictException("Bu adda bir etiket zaten var.");
        }

        var label = new Label
        {
            ProjectId = projectId,
            Name = name,
            ColorHex = request.ColorHex
        };

        context.Labels.Add(label);

        await context.SaveChangesAsync(cancellationToken);

        return new LabelDto(label.Id, label.Name, label.ColorHex);
    }

    public async Task DeleteAsync(
        Guid projectId,
        Guid labelId,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureCanManageProjectAsync(projectId, cancellationToken);

        var label = await context.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId && l.ProjectId == projectId, cancellationToken)
            ?? throw new NotFoundException("Etiket", labelId);

        // Görev ilişkileri cascade ile temizlenir.
        context.Labels.Remove(label);

        await context.SaveChangesAsync(cancellationToken);
    }
}
