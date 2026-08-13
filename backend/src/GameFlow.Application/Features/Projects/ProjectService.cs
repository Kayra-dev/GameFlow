using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Projects.Dtos;
using GameFlow.Application.Features.Sprints.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Projects;

/// <summary>
/// Proje yönetimi. Proje oluşturma ve silme yalnızca yöneticilere; ayar güncelleme
/// ve üye yönetimi yönetici ile proje yöneticilerine açıktır.
/// Listeleme, kullanıcının yalnızca üyesi olduğu projeleri döndürecek şekilde kısıtlanır.
/// </summary>
public class ProjectService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IPermissionService permissions,
    IDateTimeProvider dateTime,
    IActivityLogger activityLogger) : IProjectService
{
    public async Task<IReadOnlyList<ProjectSummaryDto>> GetListAsync(
        ProjectListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Projects.AsNoTracking();

        // Yönetici olmayan kullanıcılar yalnızca üyesi olduğu projeleri görür.
        if (request.OnlyMine || !currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();
            query = query.Where(p => p.Members.Any(m => m.UserId == userId));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Key.ToLower().Contains(term));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        return await query
            .OrderBy(p => p.Status)
            .ThenBy(p => p.Name)
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Name,
                p.Key,
                p.Status,
                p.ColorHex,
                p.CoverImageUrl,
                p.Members.Count,
                p.WorkItems.Count,
                p.WorkItems.Count(w => w.Status == WorkItemStatus.Done)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectDetailDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureProjectMemberAsync(id, cancellationToken);

        var now = dateTime.UtcNow;

        var project = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectDetailDto(
                p.Id,
                p.Name,
                p.Key,
                p.Status,
                p.ColorHex,
                p.CoverImageUrl,
                p.Members.Count,
                p.WorkItems.Count,
                p.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                p.Description,
                p.Genre,
                p.Platforms,
                p.StartDate,
                p.TargetReleaseDate,
                p.CreatedAt,
                p.Members
                    .OrderByDescending(m => m.IsManager)
                    .ThenBy(m => m.User.FullName)
                    .Select(m => new ProjectMemberDto(
                        m.Id,
                        new UserSummaryDto(
                            m.User.Id,
                            m.User.FullName,
                            m.User.Email,
                            m.User.JobTitle,
                            m.User.AvatarUrl,
                            (SystemRole)m.User.RoleId,
                            m.User.IsOnline,
                            m.User.LastSeenAt),
                        m.IsManager,
                        m.JoinedAt))
                    .ToList(),
                p.Sprints
                    .Where(s => s.Status == SprintStatus.Active)
                    .Select(s => new SprintSummaryDto(
                        s.Id,
                        s.Name,
                        s.Status,
                        s.StartDate,
                        s.EndDate,
                        s.WorkItems.Count,
                        s.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                        s.WorkItems.Count(w => w.Status != WorkItemStatus.Cancelled) == 0
                            ? 0
                            : s.WorkItems.Count(w => w.Status == WorkItemStatus.Done) * 100
                              / s.WorkItems.Count(w => w.Status != WorkItemStatus.Cancelled)))
                    .FirstOrDefault(),
                p.WorkItems.Count(w =>
                    w.DueDate != null
                    && w.DueDate < now
                    && w.Status != WorkItemStatus.Done
                    && w.Status != WorkItemStatus.Cancelled),
                p.WorkItems.Count(w => w.Status != WorkItemStatus.Cancelled) == 0
                    ? 0
                    : p.WorkItems.Count(w => w.Status == WorkItemStatus.Done) * 100
                      / p.WorkItems.Count(w => w.Status != WorkItemStatus.Cancelled)))
            .FirstOrDefaultAsync(cancellationToken);

        return project ?? throw new NotFoundException("Proje", id);
    }

    public async Task<ProjectDetailDto> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var key = request.Key.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

        if (await context.Projects.IgnoreQueryFilters().AnyAsync(p => p.Key == key, cancellationToken))
        {
            throw new ConflictException($"'{key}' anahtarı başka bir projede kullanılıyor.");
        }

        // Projeyi oluşturan kişi otomatik olarak proje yöneticisi üye olur.
        var creatorId = currentUser.RequireUserId();
        var memberIds = request.MemberIds.ToHashSet();
        memberIds.Add(creatorId);

        await EnsureUsersExistAsync(memberIds, cancellationToken);

        var project = new Project
        {
            Name = name,
            Key = key,
            Description = Normalize(request.Description),
            Status = request.Status,
            ColorHex = request.ColorHex,
            Genre = Normalize(request.Genre),
            Platforms = Normalize(request.Platforms),
            StartDate = request.StartDate,
            TargetReleaseDate = request.TargetReleaseDate,
            CreatedById = creatorId
        };

        context.Projects.Add(project);

        foreach (var userId in memberIds)
        {
            context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = userId,
                IsManager = userId == creatorId,
                JoinedAt = dateTime.UtcNow
            });
        }

        // Proje geneline açık sohbet odası otomatik oluşur.
        context.ChatRooms.Add(new ChatRoom
        {
            Type = ChatRoomType.Project,
            Name = project.Name,
            Description = $"{project.Name} projesinin genel sohbet odası.",
            ProjectId = project.Id,
            IsSystem = true
        });

        activityLogger.Log(
            ActivityType.ProjectCreated,
            $"{project.Name} projesi oluşturuldu.",
            projectId: project.Id,
            entityType: nameof(Project),
            entityId: project.Id);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectDetailDto> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureCanManageProjectAsync(id, cancellationToken);

        var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Proje", id);

        var name = request.Name.Trim();
        var renamed = project.Name != name;

        // Proje anahtarı görev anahtarlarına gömülü olduğu için değiştirilemez.
        project.Name = name;
        project.Description = Normalize(request.Description);
        project.Status = request.Status;
        project.ColorHex = request.ColorHex;
        project.Genre = Normalize(request.Genre);
        project.Platforms = Normalize(request.Platforms);
        project.StartDate = request.StartDate;
        project.TargetReleaseDate = request.TargetReleaseDate;

        if (renamed)
        {
            var chatRoom = await context.ChatRooms
                .FirstOrDefaultAsync(
                    r => r.ProjectId == id && r.Type == ChatRoomType.Project,
                    cancellationToken);

            if (chatRoom is not null)
            {
                chatRoom.Name = name;
            }
        }

        activityLogger.Log(
            ActivityType.ProjectUpdated,
            $"{project.Name} projesi güncellendi.",
            projectId: project.Id,
            entityType: nameof(Project),
            entityId: project.Id);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(project.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Proje", id);

        // Proje mantıksal olarak silinir. Görevler de ISoftDeletable olduğu için
        // erişilemez hâle gelmesi adına birlikte işaretlenir; sohbet geçmişi ve
        // denetim kayıtları korunur.
        var workItems = await context.WorkItems
            .Where(w => w.ProjectId == id)
            .ToListAsync(cancellationToken);

        foreach (var workItem in workItems)
        {
            context.WorkItems.Remove(workItem);
        }

        var memberships = await context.ProjectMembers
            .Where(m => m.ProjectId == id)
            .ToListAsync(cancellationToken);

        foreach (var membership in memberships)
        {
            context.ProjectMembers.Remove(membership);
        }

        context.Projects.Remove(project);

        activityLogger.Log(
            ActivityType.ProjectDeleted,
            $"{project.Name} projesi silindi.",
            entityType: nameof(Project),
            entityId: project.Id);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectDetailDto> AddMembersAsync(
        Guid id,
        AddProjectMembersRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureCanManageProjectAsync(id, cancellationToken);

        var project = await context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Proje", id);

        var requestedIds = request.UserIds.Distinct().ToList();

        await EnsureUsersExistAsync(requestedIds, cancellationToken);

        var existingIds = await context.ProjectMembers
            .Where(m => m.ProjectId == id)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var newIds = requestedIds.Except(existingIds).ToList();

        if (newIds.Count == 0)
        {
            throw new DomainException("Seçilen kullanıcılar bu projede zaten var.");
        }

        foreach (var userId in newIds)
        {
            context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = id,
                UserId = userId,
                IsManager = request.IsManager,
                JoinedAt = dateTime.UtcNow
            });
        }

        activityLogger.Log(
            ActivityType.ProjectUpdated,
            $"{project.Name} projesine {newIds.Count} yeni üye eklendi.",
            projectId: id,
            entityType: nameof(Project),
            entityId: id);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task RemoveMemberAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureCanManageProjectAsync(id, cancellationToken);

        var membership = await context.ProjectMembers
            .Include(m => m.User)
            .Include(m => m.Project)
            .FirstOrDefaultAsync(m => m.ProjectId == id && m.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Proje üyeliği", userId);

        if (membership.IsManager)
        {
            await EnsureNotLastManagerAsync(id, userId, cancellationToken);
        }

        // Kullanıcının bu projedeki açık görevleri atamasız kalır.
        var assignedWorkItems = await context.WorkItems
            .Where(w => w.ProjectId == id
                        && w.AssigneeId == userId
                        && w.Status != WorkItemStatus.Done
                        && w.Status != WorkItemStatus.Cancelled)
            .ToListAsync(cancellationToken);

        foreach (var workItem in assignedWorkItems)
        {
            workItem.AssigneeId = null;
        }

        context.ProjectMembers.Remove(membership);

        activityLogger.Log(
            ActivityType.ProjectUpdated,
            $"{membership.User.FullName}, {membership.Project.Name} projesinden çıkarıldı.",
            projectId: id,
            entityType: nameof(Project),
            entityId: id);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetMemberManagerAsync(
        Guid id,
        Guid userId,
        bool isManager,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureCanManageProjectAsync(id, cancellationToken);

        var membership = await context.ProjectMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.ProjectId == id && m.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Proje üyeliği", userId);

        if (membership.IsManager && !isManager)
        {
            await EnsureNotLastManagerAsync(id, userId, cancellationToken);
        }

        membership.IsManager = isManager;

        activityLogger.Log(
            ActivityType.ProjectUpdated,
            isManager
                ? $"{membership.User.FullName} proje yöneticisi yapıldı."
                : $"{membership.User.FullName} kullanıcısının proje yöneticiliği kaldırıldı.",
            projectId: id,
            entityType: nameof(Project),
            entityId: id);

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Projenin yönetimsiz kalmasını engeller. Sistem yöneticileri her projeyi
    /// yönetebildiği için bu kural yalnızca proje düzeyindeki yetkiyi korur.
    /// </summary>
    private async Task EnsureNotLastManagerAsync(
        Guid projectId,
        Guid excludedUserId,
        CancellationToken cancellationToken)
    {
        var otherManagerExists = await context.ProjectMembers
            .AnyAsync(
                m => m.ProjectId == projectId && m.UserId != excludedUserId && m.IsManager,
                cancellationToken);

        if (!otherManagerExists)
        {
            throw new DomainException(
                "Projedeki son yöneticiyi kaldıramazsınız. Önce başka bir proje yöneticisi belirleyin.");
        }
    }

    private async Task EnsureUsersExistAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var foundCount = await context.Users
            .CountAsync(u => userIds.Contains(u.Id) && u.IsActive, cancellationToken);

        if (foundCount != userIds.Distinct().Count())
        {
            throw new NotFoundException("Kullanıcı", string.Join(", ", userIds));
        }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
