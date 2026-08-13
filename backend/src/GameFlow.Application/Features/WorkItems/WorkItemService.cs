using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Common.Models;
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
/// Görev yönetimi ve kanban panosu.
///
/// Yetki kuralları:
/// <list type="bullet">
///   <item>Görüntüleme: projenin üyesi olmak yeterli.</item>
///   <item>Oluşturma/silme: yönetici, proje yöneticisi veya görevin takım lideri.</item>
///   <item>Alan güncelleme: yukarıdakiler ve görevi oluşturan kişi.</item>
///   <item>Durum değiştirme: yukarıdakiler ve göreve atanan kişi.</item>
/// </list>
/// </summary>
public class WorkItemService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IPermissionService permissions,
    INotificationService notifications,
    IActivityLogger activityLogger,
    IDateTimeProvider dateTime) : IWorkItemService
{
    /// <summary>Kanban sıralamasında iki kart arasında bırakılan boşluk.</summary>
    private const double BoardOrderStep = 1024d;

    /// <summary>
    /// Kayan nokta hassasiyeti tükenmeye başladığında kolon yeniden dengelenir.
    /// </summary>
    private const double MinimumBoardOrderGap = 0.001d;

    /// <summary>Görev detayında gösterilecek en fazla aktivite kaydı.</summary>
    private const int ActivityHistoryLimit = 50;

    public async Task<PagedResult<WorkItemSummaryDto>> GetListAsync(
        WorkItemListRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = dateTime.UtcNow;
        var query = await BuildScopedQueryAsync(cancellationToken);

        if (request.ProjectId.HasValue)
        {
            query = query.Where(w => w.ProjectId == request.ProjectId.Value);
        }

        if (request.TeamId.HasValue)
        {
            query = query.Where(w => w.TeamId == request.TeamId.Value);
        }

        if (request.SprintId.HasValue)
        {
            query = query.Where(w => w.SprintId == request.SprintId.Value);
        }

        if (request.OnlyMine)
        {
            var userId = currentUser.RequireUserId();
            query = query.Where(w => w.AssigneeId == userId);
        }
        else if (request.AssigneeId.HasValue)
        {
            query = query.Where(w => w.AssigneeId == request.AssigneeId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(w => w.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(w => w.Priority == request.Priority.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(w => w.Type == request.Type.Value);
        }

        if (request.LabelId.HasValue)
        {
            query = query.Where(w => w.Labels.Any(l => l.LabelId == request.LabelId.Value));
        }

        if (request.OnlyActive)
        {
            query = query.Where(w =>
                w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Cancelled);
        }

        if (request.OnlyOverdue)
        {
            query = query.Where(w =>
                w.DueDate != null
                && w.DueDate < now
                && w.Status != WorkItemStatus.Done
                && w.Status != WorkItemStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(w =>
                w.Title.ToLower().Contains(term) || w.Key.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, request);

        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(WorkItemProjections.ToSummary(now))
            .ToListAsync(cancellationToken);

        return PagedResult<WorkItemSummaryDto>.Create(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<WorkItemDetailDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var detail = await QueryDetailAsync(w => w.Id == id, cancellationToken);

        return detail ?? throw new NotFoundException("Görev", id);
    }

    public async Task<WorkItemDetailDto> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim().ToUpperInvariant();
        var detail = await QueryDetailAsync(w => w.Key == normalized, cancellationToken);

        return detail ?? throw new NotFoundException("Görev", key);
    }

    public async Task<KanbanBoardDto> GetBoardAsync(
        Guid projectId,
        Guid? teamId = null,
        Guid? sprintId = null,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureProjectMemberAsync(projectId, cancellationToken);

        var project = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.Id, p.Key })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Proje", projectId);

        var now = dateTime.UtcNow;

        var query = context.WorkItems
            .AsNoTracking()
            .Where(w => w.ProjectId == projectId && w.ParentId == null);

        if (teamId.HasValue)
        {
            query = query.Where(w => w.TeamId == teamId.Value);
        }

        if (sprintId.HasValue)
        {
            query = query.Where(w => w.SprintId == sprintId.Value);
        }

        // Pano tek sorguda çekilir, kolonlara bellekte bölünür. Kolon başına ayrı
        // sorgu açmak yedi ayrı gidiş-dönüş anlamına gelirdi.
        var items = await query
            .OrderBy(w => w.Status)
            .ThenBy(w => w.BoardOrder)
            .Select(WorkItemProjections.ToSummary(now))
            .ToListAsync(cancellationToken);

        var grouped = items.GroupBy(item => item.Status).ToDictionary(g => g.Key, g => g.ToList());

        var columns = WorkItemProjections.BoardColumnOrder
            .Select(status =>
            {
                var columnItems = grouped.GetValueOrDefault(status) ?? [];

                return new KanbanColumnDto(
                    status,
                    WorkItemProjections.GetStatusLabel(status),
                    columnItems.Count,
                    columnItems);
            })
            .ToList();

        return new KanbanBoardDto(project.Id, project.Key, columns);
    }

    public async Task<WorkItemDetailDto> CreateAsync(
        CreateWorkItemRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanCreateAsync(request.ProjectId, request.TeamId, cancellationToken);

        var project = await context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Proje", request.ProjectId);

        await ValidateRelationsAsync(
            request.ProjectId,
            request.AssigneeId,
            request.TeamId,
            request.SprintId,
            request.ParentId,
            request.LabelIds,
            cancellationToken);

        var reporterId = currentUser.RequireUserId();
        var number = await context.GetNextWorkItemNumberAsync(request.ProjectId, cancellationToken);
        var now = dateTime.UtcNow;

        var workItem = new WorkItem
        {
            ProjectId = request.ProjectId,
            TeamId = request.TeamId,
            SprintId = request.SprintId,
            ParentId = request.ParentId,
            Number = number,
            Key = $"{project.Key}-{number}",
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            Status = request.Status,
            Priority = request.Priority,
            Type = request.Type,
            AssigneeId = request.AssigneeId,
            ReporterId = reporterId,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            EstimatedHours = request.EstimatedHours,
            StoryPoints = request.StoryPoints,
            CompletedAt = request.Status == WorkItemStatus.Done ? now : null,
            BoardOrder = await GetTopBoardOrderAsync(request.ProjectId, request.Status, cancellationToken)
        };

        context.WorkItems.Add(workItem);

        foreach (var labelId in request.LabelIds.Distinct())
        {
            context.WorkItemLabels.Add(new WorkItemLabel { WorkItemId = workItem.Id, LabelId = labelId });
        }

        var order = 0;

        foreach (var text in request.ChecklistItems.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            context.TaskChecklistItems.Add(new TaskChecklistItem
            {
                WorkItemId = workItem.Id,
                Text = text.Trim(),
                Order = order++
            });
        }

        activityLogger.Log(
            ActivityType.TaskCreated,
            $"{workItem.Key} görevi oluşturuldu: {workItem.Title}",
            projectId: workItem.ProjectId,
            teamId: workItem.TeamId,
            workItemId: workItem.Id,
            entityType: nameof(WorkItem),
            entityId: workItem.Id);

        if (workItem.AssigneeId.HasValue)
        {
            notifications.Queue(new NotificationRequest(
                workItem.AssigneeId.Value,
                NotificationType.TaskAssigned,
                "Yeni görev atandı",
                $"{workItem.Key} · {workItem.Title}",
                $"/gorevler/{workItem.Key}",
                nameof(WorkItem),
                workItem.Id));
        }

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetByIdAsync(workItem.Id, cancellationToken);
    }

    public async Task<WorkItemDetailDto> UpdateAsync(
        Guid id,
        UpdateWorkItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var workItem = await context.WorkItems
            .Include(w => w.Labels)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException("Görev", id);

        await EnsureCanEditAsync(workItem, cancellationToken);

        await ValidateRelationsAsync(
            workItem.ProjectId,
            request.AssigneeId,
            request.TeamId,
            request.SprintId,
            parentId: null,
            request.LabelIds,
            cancellationToken);

        var previousAssigneeId = workItem.AssigneeId;

        workItem.Title = request.Title.Trim();
        workItem.Description = Normalize(request.Description);
        workItem.Priority = request.Priority;
        workItem.Type = request.Type;
        workItem.AssigneeId = request.AssigneeId;
        workItem.TeamId = request.TeamId;
        workItem.SprintId = request.SprintId;
        workItem.StartDate = request.StartDate;
        workItem.DueDate = request.DueDate;
        workItem.EstimatedHours = request.EstimatedHours;
        workItem.LoggedHours = request.LoggedHours;
        workItem.StoryPoints = request.StoryPoints;

        SyncLabels(workItem, request.LabelIds);

        activityLogger.Log(
            ActivityType.TaskUpdated,
            $"{workItem.Key} görevi güncellendi.",
            projectId: workItem.ProjectId,
            teamId: workItem.TeamId,
            workItemId: workItem.Id,
            entityType: nameof(WorkItem),
            entityId: workItem.Id);

        // Atama değiştiyse yeni kişiye bildirim gider.
        if (workItem.AssigneeId.HasValue && workItem.AssigneeId != previousAssigneeId)
        {
            notifications.Queue(new NotificationRequest(
                workItem.AssigneeId.Value,
                NotificationType.TaskAssigned,
                "Size bir görev atandı",
                $"{workItem.Key} · {workItem.Title}",
                $"/gorevler/{workItem.Key}",
                nameof(WorkItem),
                workItem.Id));
        }
        else if (workItem.AssigneeId.HasValue)
        {
            notifications.Queue(new NotificationRequest(
                workItem.AssigneeId.Value,
                NotificationType.TaskUpdated,
                "Görev güncellendi",
                $"{workItem.Key} · {workItem.Title}",
                $"/gorevler/{workItem.Key}",
                nameof(WorkItem),
                workItem.Id));
        }

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetByIdAsync(workItem.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workItem = await context.WorkItems.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
                       ?? throw new NotFoundException("Görev", id);

        await EnsureCanDeleteAsync(workItem, cancellationToken);

        // Alt görevler de birlikte silinir; yetim kalmamaları gerekir.
        var subItems = await context.WorkItems
            .Where(w => w.ParentId == id)
            .ToListAsync(cancellationToken);

        foreach (var subItem in subItems)
        {
            context.WorkItems.Remove(subItem);
        }

        context.WorkItems.Remove(workItem);

        activityLogger.Log(
            ActivityType.TaskDeleted,
            $"{workItem.Key} görevi silindi.",
            projectId: workItem.ProjectId,
            teamId: workItem.TeamId,
            entityType: nameof(WorkItem),
            entityId: workItem.Id);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkItemSummaryDto> MoveAsync(
        Guid id,
        MoveWorkItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var workItem = await context.WorkItems.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
                       ?? throw new NotFoundException("Görev", id);

        await EnsureCanChangeStatusAsync(workItem, cancellationToken);

        var previousStatus = workItem.Status;

        workItem.BoardOrder = await CalculateBoardOrderAsync(
            workItem.ProjectId,
            request.TargetStatus,
            request.PrecedingItemId,
            request.FollowingItemId,
            cancellationToken);

        ApplyStatusChange(workItem, request.TargetStatus);

        if (previousStatus != request.TargetStatus)
        {
            LogStatusChange(workItem, previousStatus);
            QueueStatusChangeNotifications(workItem, previousStatus);
        }

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetSummaryAsync(workItem.Id, cancellationToken);
    }

    public async Task<WorkItemSummaryDto> ChangeStatusAsync(
        Guid id,
        ChangeStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var workItem = await context.WorkItems.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
                       ?? throw new NotFoundException("Görev", id);

        await EnsureCanChangeStatusAsync(workItem, cancellationToken);

        var previousStatus = workItem.Status;

        if (previousStatus == request.Status)
        {
            return await GetSummaryAsync(workItem.Id, cancellationToken);
        }

        // Kolon değiştiğinde kart hedef kolonun en üstüne yerleşir.
        workItem.BoardOrder = await GetTopBoardOrderAsync(
            workItem.ProjectId,
            request.Status,
            cancellationToken);

        ApplyStatusChange(workItem, request.Status);
        LogStatusChange(workItem, previousStatus);
        QueueStatusChangeNotifications(workItem, previousStatus);

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetSummaryAsync(workItem.Id, cancellationToken);
    }

    public async Task<WorkItemSummaryDto> AssignAsync(
        Guid id,
        AssignWorkItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var workItem = await context.WorkItems.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
                       ?? throw new NotFoundException("Görev", id);

        await EnsureCanEditAsync(workItem, cancellationToken);

        if (request.AssigneeId.HasValue)
        {
            await EnsureUserIsProjectMemberAsync(
                workItem.ProjectId,
                request.AssigneeId.Value,
                cancellationToken);
        }

        var previousAssigneeId = workItem.AssigneeId;
        workItem.AssigneeId = request.AssigneeId;

        if (previousAssigneeId == request.AssigneeId)
        {
            return await GetSummaryAsync(workItem.Id, cancellationToken);
        }

        var assigneeName = request.AssigneeId is null
            ? null
            : await context.Users
                .Where(u => u.Id == request.AssigneeId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);

        activityLogger.Log(
            ActivityType.TaskAssigned,
            assigneeName is null
                ? $"{workItem.Key} görevinin ataması kaldırıldı."
                : $"{workItem.Key} görevi {assigneeName} kişisine atandı.",
            projectId: workItem.ProjectId,
            teamId: workItem.TeamId,
            workItemId: workItem.Id,
            entityType: nameof(WorkItem),
            entityId: workItem.Id);

        if (request.AssigneeId.HasValue)
        {
            notifications.Queue(new NotificationRequest(
                request.AssigneeId.Value,
                NotificationType.TaskAssigned,
                "Size bir görev atandı",
                $"{workItem.Key} · {workItem.Title}",
                $"/gorevler/{workItem.Key}",
                nameof(WorkItem),
                workItem.Id));
        }

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetSummaryAsync(workItem.Id, cancellationToken);
    }

    public async Task<DeadlineOverviewDto> GetDeadlineOverviewAsync(
        Guid? projectId = null,
        Guid? teamId = null,
        int upcomingDays = 7,
        bool onlyMine = false,
        CancellationToken cancellationToken = default)
    {
        var now = dateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var upcomingEnd = todayStart.AddDays(Math.Clamp(upcomingDays, 1, 90) + 1);

        var baseQuery = (await BuildScopedQueryAsync(cancellationToken))
            .Where(w =>
                w.DueDate != null
                && w.Status != WorkItemStatus.Done
                && w.Status != WorkItemStatus.Cancelled);

        if (projectId.HasValue)
        {
            baseQuery = baseQuery.Where(w => w.ProjectId == projectId.Value);
        }

        if (teamId.HasValue)
        {
            baseQuery = baseQuery.Where(w => w.TeamId == teamId.Value);
        }

        if (onlyMine)
        {
            var userId = currentUser.RequireUserId();
            baseQuery = baseQuery.Where(w => w.AssigneeId == userId);
        }

        var projection = WorkItemProjections.ToSummary(now);

        var dueToday = await baseQuery
            .Where(w => w.DueDate >= todayStart && w.DueDate < todayEnd)
            .OrderBy(w => w.DueDate)
            .Select(projection)
            .ToListAsync(cancellationToken);

        var upcoming = await baseQuery
            .Where(w => w.DueDate >= todayEnd && w.DueDate < upcomingEnd)
            .OrderBy(w => w.DueDate)
            .Select(projection)
            .ToListAsync(cancellationToken);

        var overdue = await baseQuery
            .Where(w => w.DueDate < todayStart)
            .OrderBy(w => w.DueDate)
            .Select(projection)
            .ToListAsync(cancellationToken);

        return new DeadlineOverviewDto(dueToday, upcoming, overdue);
    }

    // ---------------------------------------------------------------- Sorgular

    /// <summary>
    /// Kullanıcının erişebildiği görevleri kapsayan temel sorgu.
    /// Yönetici tüm görevleri görür; diğerleri yalnızca üyesi olduğu projelerdekileri.
    /// </summary>
    private async Task<IQueryable<WorkItem>> BuildScopedQueryAsync(CancellationToken cancellationToken)
    {
        var query = context.WorkItems.AsNoTracking();

        if (currentUser.IsAdmin)
        {
            return query;
        }

        var userId = currentUser.RequireUserId();

        var projectIds = await context.ProjectMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.ProjectId)
            .ToListAsync(cancellationToken);

        return query.Where(w => projectIds.Contains(w.ProjectId));
    }

    private static IQueryable<WorkItem> ApplySorting(
        IQueryable<WorkItem> query,
        WorkItemListRequest request)
        => (request.SortBy, request.SortDescending) switch
        {
            (WorkItemSortField.DueDate, false) => query.OrderBy(w => w.DueDate == null)
                .ThenBy(w => w.DueDate),
            (WorkItemSortField.DueDate, true) => query.OrderBy(w => w.DueDate == null)
                .ThenByDescending(w => w.DueDate),
            (WorkItemSortField.Priority, false) => query.OrderBy(w => w.Priority),
            (WorkItemSortField.Priority, true) => query.OrderByDescending(w => w.Priority),
            (WorkItemSortField.Status, false) => query.OrderBy(w => w.Status).ThenBy(w => w.BoardOrder),
            (WorkItemSortField.Status, true) => query.OrderByDescending(w => w.Status)
                .ThenBy(w => w.BoardOrder),
            (WorkItemSortField.Title, false) => query.OrderBy(w => w.Title),
            (WorkItemSortField.Title, true) => query.OrderByDescending(w => w.Title),
            (_, false) => query.OrderBy(w => w.CreatedAt),
            (_, true) => query.OrderByDescending(w => w.CreatedAt)
        };

    private async Task<WorkItemSummaryDto> GetSummaryAsync(Guid id, CancellationToken cancellationToken)
    {
        var now = dateTime.UtcNow;

        return await context.WorkItems
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(WorkItemProjections.ToSummary(now))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Görev", id);
    }

    private async Task<WorkItemDetailDto?> QueryDetailAsync(
        System.Linq.Expressions.Expression<Func<WorkItem, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var now = dateTime.UtcNow;

        var projectId = await context.WorkItems
            .AsNoTracking()
            .Where(predicate)
            .Select(w => (Guid?)w.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        if (projectId is null)
        {
            return null;
        }

        // Görüntüleme için proje üyeliği yeterli.
        await permissions.EnsureProjectMemberAsync(projectId.Value, cancellationToken);

        return await context.WorkItems
            .AsNoTracking()
            .Where(predicate)
            .Select(w => new WorkItemDetailDto(
                w.Id,
                w.Key,
                w.Title,
                w.Status,
                w.Priority,
                w.Type,
                w.StartDate,
                w.DueDate,
                w.BoardOrder,
                w.Assignee == null
                    ? null
                    : new UserSummaryDto(
                        w.Assignee.Id,
                        w.Assignee.FullName,
                        w.Assignee.Email,
                        w.Assignee.JobTitle,
                        w.Assignee.AvatarUrl,
                        (SystemRole)w.Assignee.RoleId,
                        w.Assignee.IsOnline,
                        w.Assignee.LastSeenAt),
                w.ProjectId,
                w.Project.Key,
                w.Project.Name,
                w.TeamId,
                w.Team == null ? null : w.Team.Name,
                w.SprintId,
                w.Sprint == null ? null : w.Sprint.Name,
                w.StoryPoints,
                w.Labels
                    .Select(l => new LabelDto(l.Label.Id, l.Label.Name, l.Label.ColorHex))
                    .ToList(),
                w.Comments.Count,
                w.Attachments.Count,
                w.ChecklistItems.Count,
                w.ChecklistItems.Count(c => c.IsCompleted),
                w.DueDate == null ? null : (int)(w.DueDate.Value.Date - now.Date).TotalDays,
                w.DueDate != null
                && w.DueDate < now
                && w.Status != WorkItemStatus.Done
                && w.Status != WorkItemStatus.Cancelled,
                w.Description,
                w.EstimatedHours,
                w.LoggedHours,
                w.CompletedAt,
                w.CreatedAt,
                w.UpdatedAt,
                w.Reporter == null
                    ? null
                    : new UserSummaryDto(
                        w.Reporter.Id,
                        w.Reporter.FullName,
                        w.Reporter.Email,
                        w.Reporter.JobTitle,
                        w.Reporter.AvatarUrl,
                        (SystemRole)w.Reporter.RoleId,
                        w.Reporter.IsOnline,
                        w.Reporter.LastSeenAt),
                w.ParentId,
                w.Parent == null ? null : w.Parent.Key,
                // Alt görevler için paylaşılan projeksiyon ifadesi kullanılamaz:
                // iç içe koleksiyonlar IEnumerable olduğu için EF Core burada
                // Expression yerine yerinde yazılmış bir yansıtma bekler.
                // Alt görevler tek seviye olduğundan kendi alt görev sayısı 0'dır.
                w.SubItems
                    .OrderBy(s => s.BoardOrder)
                    .Select(s => new WorkItemSummaryDto(
                        s.Id,
                        s.Key,
                        s.Title,
                        s.Status,
                        s.Priority,
                        s.Type,
                        s.StartDate,
                        s.DueDate,
                        s.BoardOrder,
                        s.Assignee == null
                            ? null
                            : new UserSummaryDto(
                                s.Assignee.Id,
                                s.Assignee.FullName,
                                s.Assignee.Email,
                                s.Assignee.JobTitle,
                                s.Assignee.AvatarUrl,
                                (SystemRole)s.Assignee.RoleId,
                                s.Assignee.IsOnline,
                                s.Assignee.LastSeenAt),
                        s.ProjectId,
                        w.Project.Key,
                        w.Project.Name,
                        s.TeamId,
                        s.Team == null ? null : s.Team.Name,
                        s.SprintId,
                        s.StoryPoints,
                        s.Labels
                            .Select(l => new LabelDto(l.Label.Id, l.Label.Name, l.Label.ColorHex))
                            .ToList(),
                        s.Comments.Count,
                        s.Attachments.Count,
                        s.ChecklistItems.Count,
                        s.ChecklistItems.Count(c => c.IsCompleted),
                        0,
                        s.DueDate == null ? null : (int)(s.DueDate.Value.Date - now.Date).TotalDays,
                        s.DueDate != null
                        && s.DueDate < now
                        && s.Status != WorkItemStatus.Done
                        && s.Status != WorkItemStatus.Cancelled))
                    .ToList(),
                w.ChecklistItems
                    .OrderBy(c => c.Order)
                    .Select(c => new ChecklistItemDto(c.Id, c.Text, c.IsCompleted, c.Order, c.CompletedAt))
                    .ToList(),
                w.Attachments
                    .OrderByDescending(a => a.CreatedAt)
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
                    .ToList(),
                w.Comments
                    .OrderBy(c => c.CreatedAt)
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
                    .ToList(),
                w.Activities
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(ActivityHistoryLimit)
                    .Select(a => new ActivityDto(
                        a.Id,
                        a.Type,
                        a.Description,
                        a.Actor == null
                            ? null
                            : new UserSummaryDto(
                                a.Actor.Id,
                                a.Actor.FullName,
                                a.Actor.Email,
                                a.Actor.JobTitle,
                                a.Actor.AvatarUrl,
                                (SystemRole)a.Actor.RoleId,
                                a.Actor.IsOnline,
                                a.Actor.LastSeenAt),
                        a.CreatedAt))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // ------------------------------------------------------- Kanban sıralaması

    /// <summary>Hedef kolonun en üstüne yerleşecek sıra değerini üretir.</summary>
    private async Task<double> GetTopBoardOrderAsync(
        Guid projectId,
        WorkItemStatus status,
        CancellationToken cancellationToken)
    {
        var minimum = await context.WorkItems
            .Where(w => w.ProjectId == projectId && w.Status == status)
            .MinAsync(w => (double?)w.BoardOrder, cancellationToken);

        return (minimum ?? 0d) - BoardOrderStep;
    }

    /// <summary>
    /// Sürükle-bırak sonrası sıra değeri: iki komşunun ortası alınır, böylece
    /// yalnızca taşınan satır güncellenir. Boşluk tükenirse kolon yeniden dengelenir.
    /// </summary>
    private async Task<double> CalculateBoardOrderAsync(
        Guid projectId,
        WorkItemStatus targetStatus,
        Guid? precedingItemId,
        Guid? followingItemId,
        CancellationToken cancellationToken)
    {
        var preceding = precedingItemId.HasValue
            ? await GetBoardOrderAsync(precedingItemId.Value, cancellationToken)
            : null;

        var following = followingItemId.HasValue
            ? await GetBoardOrderAsync(followingItemId.Value, cancellationToken)
            : null;

        if (preceding is null && following is null)
        {
            return await GetTopBoardOrderAsync(projectId, targetStatus, cancellationToken);
        }

        if (preceding is null)
        {
            return following!.Value - BoardOrderStep;
        }

        if (following is null)
        {
            return preceding.Value + BoardOrderStep;
        }

        if (Math.Abs(following.Value - preceding.Value) > MinimumBoardOrderGap)
        {
            return (preceding.Value + following.Value) / 2d;
        }

        // Hassasiyet tükendi: kolonu eşit aralıklarla yeniden numaralandır ve
        // taşınan kartı hedef komşunun hemen ardına yerleştir.
        return await RebalanceColumnAsync(
            projectId,
            targetStatus,
            precedingItemId!.Value,
            cancellationToken);
    }

    private async Task<double?> GetBoardOrderAsync(Guid workItemId, CancellationToken cancellationToken)
        => await context.WorkItems
            .AsNoTracking()
            .Where(w => w.Id == workItemId)
            .Select(w => (double?)w.BoardOrder)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<double> RebalanceColumnAsync(
        Guid projectId,
        WorkItemStatus status,
        Guid precedingItemId,
        CancellationToken cancellationToken)
    {
        var columnItems = await context.WorkItems
            .Where(w => w.ProjectId == projectId && w.Status == status)
            .OrderBy(w => w.BoardOrder)
            .ToListAsync(cancellationToken);

        var order = BoardOrderStep;
        double? precedingOrder = null;

        foreach (var item in columnItems)
        {
            item.BoardOrder = order;

            if (item.Id == precedingItemId)
            {
                precedingOrder = order;
            }

            order += BoardOrderStep;
        }

        return (precedingOrder ?? order) + (BoardOrderStep / 2d);
    }

    // ------------------------------------------------------------- Yardımcılar

    private void ApplyStatusChange(WorkItem workItem, WorkItemStatus status)
    {
        workItem.Status = status;

        // Tamamlanma zamanı yalnızca "Tamamlandı" durumunda tutulur; görev
        // kolondan geri çekilirse temizlenir ki raporlar yanlış hesaplamasın.
        workItem.CompletedAt = status == WorkItemStatus.Done ? dateTime.UtcNow : null;
    }

    private void LogStatusChange(WorkItem workItem, WorkItemStatus previousStatus)
        => activityLogger.Log(
            ActivityType.TaskStatusChanged,
            $"{workItem.Key} görevi \"{WorkItemProjections.GetStatusLabel(previousStatus)}\" " +
            $"durumundan \"{WorkItemProjections.GetStatusLabel(workItem.Status)}\" durumuna taşındı.",
            projectId: workItem.ProjectId,
            teamId: workItem.TeamId,
            workItemId: workItem.Id,
            entityType: nameof(WorkItem),
            entityId: workItem.Id,
            metadata: new { from = previousStatus.ToString(), to = workItem.Status.ToString() });

    private void QueueStatusChangeNotifications(WorkItem workItem, WorkItemStatus previousStatus)
    {
        var recipients = new HashSet<Guid>();

        if (workItem.AssigneeId.HasValue)
        {
            recipients.Add(workItem.AssigneeId.Value);
        }

        // Görevi açan kişi de akıbetini bilmek ister.
        if (workItem.ReporterId.HasValue)
        {
            recipients.Add(workItem.ReporterId.Value);
        }

        notifications.QueueMany(recipients.Select(recipientId => new NotificationRequest(
            recipientId,
            NotificationType.TaskUpdated,
            "Görev durumu değişti",
            $"{workItem.Key} · {WorkItemProjections.GetStatusLabel(previousStatus)} → " +
            WorkItemProjections.GetStatusLabel(workItem.Status),
            $"/gorevler/{workItem.Key}",
            nameof(WorkItem),
            workItem.Id)));
    }

    private static void SyncLabels(WorkItem workItem, IReadOnlyCollection<Guid> labelIds)
    {
        var desired = labelIds.Distinct().ToHashSet();

        foreach (var existing in workItem.Labels.Where(l => !desired.Contains(l.LabelId)).ToList())
        {
            workItem.Labels.Remove(existing);
        }

        var current = workItem.Labels.Select(l => l.LabelId).ToHashSet();

        foreach (var labelId in desired.Except(current))
        {
            workItem.Labels.Add(new WorkItemLabel { WorkItemId = workItem.Id, LabelId = labelId });
        }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // --------------------------------------------------------------- Yetkiler

    private async Task EnsureCanCreateAsync(
        Guid projectId,
        Guid? teamId,
        CancellationToken cancellationToken)
    {
        await permissions.EnsureProjectMemberAsync(projectId, cancellationToken);

        if (await permissions.CanManageProjectAsync(projectId, cancellationToken))
        {
            return;
        }

        // Takım lideri, kendi takımına ait görev açabilir.
        if (teamId.HasValue && await permissions.CanManageTeamAsync(teamId.Value, cancellationToken))
        {
            return;
        }

        // Takım belirtilmemişse en az bir takımın lideri olmak gerekir.
        if (!teamId.HasValue && await permissions.IsAnyTeamLeaderAsync(cancellationToken))
        {
            return;
        }

        throw new ForbiddenException(
            "Görev oluşturmak için takım lideri, proje yöneticisi veya sistem yöneticisi olmalısınız.");
    }

    private async Task EnsureCanEditAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        await permissions.EnsureProjectMemberAsync(workItem.ProjectId, cancellationToken);

        if (await CanManageWorkItemAsync(workItem, cancellationToken))
        {
            return;
        }

        if (workItem.ReporterId == currentUser.UserId)
        {
            return;
        }

        throw new ForbiddenException("Bu görevi düzenleme yetkiniz bulunmuyor.");
    }

    private async Task EnsureCanChangeStatusAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        await permissions.EnsureProjectMemberAsync(workItem.ProjectId, cancellationToken);

        // Göreve atanan kişi kendi görevinin durumunu değiştirebilir.
        if (workItem.AssigneeId == currentUser.UserId || workItem.ReporterId == currentUser.UserId)
        {
            return;
        }

        if (await CanManageWorkItemAsync(workItem, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException(
            "Yalnızca göreve atanan kişi, takım lideri veya proje yöneticisi durumu değiştirebilir.");
    }

    private async Task EnsureCanDeleteAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        await permissions.EnsureProjectMemberAsync(workItem.ProjectId, cancellationToken);

        if (await CanManageWorkItemAsync(workItem, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException("Görev silme yetkisi takım liderleri ve yöneticilere aittir.");
    }

    private async Task<bool> CanManageWorkItemAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        if (await permissions.CanManageProjectAsync(workItem.ProjectId, cancellationToken))
        {
            return true;
        }

        return workItem.TeamId.HasValue
               && await permissions.CanManageTeamAsync(workItem.TeamId.Value, cancellationToken);
    }

    /// <summary>İlişkili kayıtların var olduğunu ve aynı projeye ait olduğunu doğrular.</summary>
    private async Task ValidateRelationsAsync(
        Guid projectId,
        Guid? assigneeId,
        Guid? teamId,
        Guid? sprintId,
        Guid? parentId,
        IReadOnlyCollection<Guid> labelIds,
        CancellationToken cancellationToken)
    {
        if (assigneeId.HasValue)
        {
            await EnsureUserIsProjectMemberAsync(projectId, assigneeId.Value, cancellationToken);
        }

        if (teamId.HasValue && !await context.Teams.AnyAsync(t => t.Id == teamId.Value, cancellationToken))
        {
            throw new NotFoundException("Takım", teamId.Value);
        }

        if (sprintId.HasValue)
        {
            var sprintBelongsToProject = await context.Sprints
                .AnyAsync(s => s.Id == sprintId.Value && s.ProjectId == projectId, cancellationToken);

            if (!sprintBelongsToProject)
            {
                throw new DomainException("Seçilen sprint bu projeye ait değil.");
            }
        }

        if (parentId.HasValue)
        {
            var parentBelongsToProject = await context.WorkItems
                .AnyAsync(w => w.Id == parentId.Value && w.ProjectId == projectId, cancellationToken);

            if (!parentBelongsToProject)
            {
                throw new DomainException("Üst görev bu projeye ait değil.");
            }
        }

        if (labelIds.Count > 0)
        {
            var validLabelCount = await context.Labels
                .CountAsync(l => labelIds.Contains(l.Id) && l.ProjectId == projectId, cancellationToken);

            if (validLabelCount != labelIds.Distinct().Count())
            {
                throw new DomainException("Seçilen etiketlerden biri bu projeye ait değil.");
            }
        }
    }

    private async Task EnsureUserIsProjectMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var isMember = await context.ProjectMembers
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == userId, cancellationToken);

        if (!isMember)
        {
            throw new DomainException(
                "Görev yalnızca projenin üyelerine atanabilir. Önce kullanıcıyı projeye ekleyin.");
        }
    }
}
