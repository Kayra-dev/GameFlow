using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Notifications.Dtos;
using GameFlow.Application.Features.Sprints.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Application.Features.WorkItems;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Sprints;

/// <summary>
/// Sprint yönetimi. Oluşturma, güncelleme, başlatma ve tamamlama işlemleri
/// proje yöneticilerine ve takım liderlerine açıktır; görüntüleme proje üyeliği ister.
/// </summary>
public class SprintService(
    IApplicationDbContext context,
    IPermissionService permissions,
    ICurrentUserService currentUser,
    INotificationService notifications,
    IActivityLogger activityLogger,
    IDateTimeProvider dateTime) : ISprintService
{
    public async Task<IReadOnlyList<SprintSummaryDto>> GetListAsync(
        SprintListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Sprints.AsNoTracking();

        if (request.ProjectId.HasValue)
        {
            await permissions.EnsureProjectMemberAsync(request.ProjectId.Value, cancellationToken);
            query = query.Where(s => s.ProjectId == request.ProjectId.Value);
        }
        else if (!currentUser.IsAdmin)
        {
            // Proje belirtilmediyse yalnızca üyesi olunan projelerin sprintleri döner.
            var userId = currentUser.RequireUserId();

            var projectIds = await context.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync(cancellationToken);

            query = query.Where(s => projectIds.Contains(s.ProjectId));
        }

        if (request.TeamId.HasValue)
        {
            query = query.Where(s => s.TeamId == request.TeamId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(s => s.Status == request.Status.Value);
        }

        return await query
            .OrderBy(s => s.Status)
            .ThenByDescending(s => s.StartDate)
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
            .ToListAsync(cancellationToken);
    }

    public async Task<SprintDetailDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var projectId = await context.Sprints
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => (Guid?)s.ProjectId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Sprint", id);

        await permissions.EnsureProjectMemberAsync(projectId, cancellationToken);

        var today = dateTime.UtcNow.Date;

        return await context.Sprints
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SprintDetailDto(
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
                      / s.WorkItems.Count(w => w.Status != WorkItemStatus.Cancelled),
                s.Goal,
                s.ProjectId,
                s.Project.Key,
                s.TeamId,
                s.Team == null ? null : s.Team.Name,
                s.StartedAt,
                s.CompletedAt,
                s.RetrospectiveNotes,
                s.WorkItems.Sum(w => w.StoryPoints ?? 0),
                s.WorkItems.Where(w => w.Status == WorkItemStatus.Done).Sum(w => w.StoryPoints ?? 0),
                (int)(s.EndDate.Date - today).TotalDays))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Sprint", id);
    }

    public async Task<SprintDetailDto> CreateAsync(
        CreateSprintRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(request.ProjectId, request.TeamId, cancellationToken);

        if (request.TeamId.HasValue
            && !await context.Teams.AnyAsync(t => t.Id == request.TeamId.Value, cancellationToken))
        {
            throw new NotFoundException("Takım", request.TeamId.Value);
        }

        var sprint = new Sprint
        {
            ProjectId = request.ProjectId,
            TeamId = request.TeamId,
            Name = request.Name.Trim(),
            Goal = Normalize(request.Goal),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = SprintStatus.Planned
        };

        context.Sprints.Add(sprint);

        activityLogger.Log(
            ActivityType.SprintCreated,
            $"{sprint.Name} sprinti oluşturuldu.",
            projectId: sprint.ProjectId,
            teamId: sprint.TeamId,
            entityType: nameof(Sprint),
            entityId: sprint.Id);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(sprint.Id, cancellationToken);
    }

    public async Task<SprintDetailDto> UpdateAsync(
        Guid id,
        UpdateSprintRequest request,
        CancellationToken cancellationToken = default)
    {
        var sprint = await GetSprintAsync(id, cancellationToken);

        await EnsureCanManageAsync(sprint.ProjectId, sprint.TeamId, cancellationToken);

        if (sprint.Status == SprintStatus.Completed)
        {
            throw new DomainException("Tamamlanmış bir sprint düzenlenemez.");
        }

        sprint.Name = request.Name.Trim();
        sprint.Goal = Normalize(request.Goal);
        sprint.TeamId = request.TeamId;
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(sprint.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sprint = await GetSprintAsync(id, cancellationToken);

        await EnsureCanManageAsync(sprint.ProjectId, sprint.TeamId, cancellationToken);

        if (sprint.Status == SprintStatus.Active)
        {
            throw new DomainException(
                "Aktif bir sprint silinemez. Önce sprinti tamamlayın veya iptal edin.");
        }

        // Sprint silinince görevler kaybolmaz, backlog'a döner (FK SetNull).
        context.Sprints.Remove(sprint);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SprintDetailDto> StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sprint = await GetSprintAsync(id, cancellationToken);

        await EnsureCanManageAsync(sprint.ProjectId, sprint.TeamId, cancellationToken);

        if (sprint.Status != SprintStatus.Planned)
        {
            throw new DomainException("Yalnızca planlanmış durumdaki sprintler başlatılabilir.");
        }

        // Bir projede aynı anda birden fazla aktif sprint karışıklık yaratır.
        var activeSprintName = await context.Sprints
            .Where(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSprintName is not null)
        {
            throw new DomainException(
                $"Bu projede \"{activeSprintName}\" sprinti hâlâ aktif. Önce onu tamamlayın.");
        }

        sprint.Status = SprintStatus.Active;
        sprint.StartedAt = dateTime.UtcNow;

        activityLogger.Log(
            ActivityType.SprintStarted,
            $"{sprint.Name} sprinti başlatıldı.",
            projectId: sprint.ProjectId,
            teamId: sprint.TeamId,
            entityType: nameof(Sprint),
            entityId: sprint.Id);

        await QueueSprintNotificationsAsync(
            sprint,
            NotificationType.SprintStarted,
            "Sprint başladı",
            $"{sprint.Name} sprinti başlatıldı.",
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetByIdAsync(sprint.Id, cancellationToken);
    }

    public async Task<SprintReportDto> CompleteAsync(
        Guid id,
        CompleteSprintRequest request,
        CancellationToken cancellationToken = default)
    {
        var sprint = await GetSprintAsync(id, cancellationToken);

        await EnsureCanManageAsync(sprint.ProjectId, sprint.TeamId, cancellationToken);

        if (sprint.Status != SprintStatus.Active)
        {
            throw new DomainException("Yalnızca aktif bir sprint tamamlanabilir.");
        }

        if (request.MoveUnfinishedToSprintId.HasValue)
        {
            var targetId = request.MoveUnfinishedToSprintId.Value;

            if (targetId == sprint.Id)
            {
                throw new DomainException("Görevler tamamlanan sprintin kendisine taşınamaz.");
            }

            var targetIsValid = await context.Sprints.AnyAsync(
                s => s.Id == targetId
                     && s.ProjectId == sprint.ProjectId
                     && s.Status != SprintStatus.Completed,
                cancellationToken);

            if (!targetIsValid)
            {
                throw new DomainException(
                    "Hedef sprint aynı projede ve tamamlanmamış durumda olmalıdır.");
            }
        }

        // Bitmemiş görevler ya hedef sprinte taşınır ya da backlog'a döner.
        var unfinished = await context.WorkItems
            .Where(w => w.SprintId == sprint.Id
                        && w.Status != WorkItemStatus.Done
                        && w.Status != WorkItemStatus.Cancelled)
            .ToListAsync(cancellationToken);

        foreach (var workItem in unfinished)
        {
            workItem.SprintId = request.MoveUnfinishedToSprintId;
        }

        sprint.Status = SprintStatus.Completed;
        sprint.CompletedAt = dateTime.UtcNow;
        sprint.RetrospectiveNotes = Normalize(request.RetrospectiveNotes);

        activityLogger.Log(
            ActivityType.SprintCompleted,
            unfinished.Count == 0
                ? $"{sprint.Name} sprinti tamamlandı."
                : $"{sprint.Name} sprinti tamamlandı. {unfinished.Count} bitmemiş görev " +
                  (request.MoveUnfinishedToSprintId.HasValue ? "sonraki sprinte taşındı." : "backlog'a döndü."),
            projectId: sprint.ProjectId,
            teamId: sprint.TeamId,
            entityType: nameof(Sprint),
            entityId: sprint.Id);

        await QueueSprintNotificationsAsync(
            sprint,
            NotificationType.SprintCompleted,
            "Sprint tamamlandı",
            $"{sprint.Name} sprinti tamamlandı.",
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetReportAsync(sprint.Id, cancellationToken);
    }

    public async Task<SprintReportDto> GetReportAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var sprint = await context.Sprints
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { s.Id, s.Name, s.Status, s.StartDate, s.EndDate, s.ProjectId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Sprint", id);

        await permissions.EnsureProjectMemberAsync(sprint.ProjectId, cancellationToken);

        var now = dateTime.UtcNow;

        // Rapor tek sorguda toplanır; görev satırları istemciye taşınmaz.
        var stats = await context.WorkItems
            .AsNoTracking()
            .Where(w => w.SprintId == id)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Completed = group.Count(w => w.Status == WorkItemStatus.Done),
                Cancelled = group.Count(w => w.Status == WorkItemStatus.Cancelled),
                Overdue = group.Count(w =>
                    w.DueDate != null
                    && w.DueDate < now
                    && w.Status != WorkItemStatus.Done
                    && w.Status != WorkItemStatus.Cancelled),
                TotalPoints = group.Sum(w => w.StoryPoints ?? 0),
                CompletedPoints = group
                    .Where(w => w.Status == WorkItemStatus.Done)
                    .Sum(w => w.StoryPoints ?? 0),
                EstimatedHours = group.Sum(w => w.EstimatedHours ?? 0m),
                LoggedHours = group.Sum(w => w.LoggedHours ?? 0m)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var statusBreakdown = await context.WorkItems
            .AsNoTracking()
            .Where(w => w.SprintId == id)
            .GroupBy(w => w.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        // Gruplama anahtarı olarak varlığın kendisi kullanılamaz (EF Core çeviremez);
        // yalnızca gösterimde gereken skaler alanlarla gruplanır.
        var contributions = await context.WorkItems
            .AsNoTracking()
            .Where(w => w.SprintId == id && w.AssigneeId != null)
            .GroupBy(w => new
            {
                UserId = w.Assignee!.Id,
                w.Assignee.FullName,
                w.Assignee.Email,
                w.Assignee.JobTitle,
                w.Assignee.AvatarUrl,
                w.Assignee.RoleId,
                w.Assignee.IsOnline,
                w.Assignee.LastSeenAt
            })
            .Select(group => new SprintMemberContributionDto(
                new UserSummaryDto(
                    group.Key.UserId,
                    group.Key.FullName,
                    group.Key.Email,
                    group.Key.JobTitle,
                    group.Key.AvatarUrl,
                    (SystemRole)group.Key.RoleId,
                    group.Key.IsOnline,
                    group.Key.LastSeenAt),
                group.Count(),
                group.Count(w => w.Status == WorkItemStatus.Done),
                group.Sum(w => w.Status == WorkItemStatus.Done ? w.StoryPoints ?? 0 : 0)))
            .ToListAsync(cancellationToken);

        var total = stats?.Total ?? 0;
        var completed = stats?.Completed ?? 0;
        var cancelled = stats?.Cancelled ?? 0;

        // İptal edilen görevler başarı oranını cezalandırmamalı.
        var countable = total - cancelled;
        var progressPercent = countable == 0 ? 0 : completed * 100 / countable;

        return new SprintReportDto(
            sprint.Id,
            sprint.Name,
            sprint.Status,
            sprint.StartDate,
            sprint.EndDate,
            total,
            completed,
            cancelled,
            countable - completed,
            stats?.Overdue ?? 0,
            stats?.TotalPoints ?? 0,
            stats?.CompletedPoints ?? 0,
            progressPercent,
            progressPercent,
            stats?.EstimatedHours ?? 0m,
            stats?.LoggedHours ?? 0m,
            statusBreakdown
                .OrderBy(item => item.Status)
                .Select(item => new SprintStatusBreakdownDto(
                    item.Status,
                    WorkItemProjections.GetStatusLabel(item.Status),
                    item.Count))
                .ToList(),
            contributions
                .OrderByDescending(contribution => contribution.CompletedCount)
                .ToList());
    }

    // ------------------------------------------------------------- Yardımcılar

    private async Task<Sprint> GetSprintAsync(Guid id, CancellationToken cancellationToken)
        => await context.Sprints.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
           ?? throw new NotFoundException("Sprint", id);

    /// <summary>
    /// Sprint yönetimi: proje yöneticisi, ilgili takımın lideri veya sistem yöneticisi.
    /// Takım belirtilmemişse en az bir takımın lideri olmak yeterlidir.
    /// </summary>
    private async Task EnsureCanManageAsync(
        Guid projectId,
        Guid? teamId,
        CancellationToken cancellationToken)
    {
        await permissions.EnsureProjectMemberAsync(projectId, cancellationToken);

        if (await permissions.CanManageProjectAsync(projectId, cancellationToken))
        {
            return;
        }

        if (teamId.HasValue && await permissions.CanManageTeamAsync(teamId.Value, cancellationToken))
        {
            return;
        }

        if (!teamId.HasValue && await permissions.IsAnyTeamLeaderAsync(cancellationToken))
        {
            return;
        }

        throw new ForbiddenException(
            "Sprint yönetimi için takım lideri, proje yöneticisi veya sistem yöneticisi olmalısınız.");
    }

    /// <summary>Sprintin kapsadığı kişilere (takım üyeleri veya proje üyeleri) bildirim kuyruğa alır.</summary>
    private async Task QueueSprintNotificationsAsync(
        Sprint sprint,
        NotificationType type,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var recipientIds = sprint.TeamId.HasValue
            ? await context.TeamMembers
                .Where(m => m.TeamId == sprint.TeamId.Value)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken)
            : await context.ProjectMembers
                .Where(m => m.ProjectId == sprint.ProjectId)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken);

        notifications.QueueMany(recipientIds.Select(recipientId => new NotificationRequest(
            recipientId,
            type,
            title,
            message,
            $"/sprintler/{sprint.Id}",
            nameof(Sprint),
            sprint.Id)));
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
