using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Announcements;
using GameFlow.Application.Features.Announcements.Dtos;
using GameFlow.Application.Features.Calendar;
using GameFlow.Application.Features.Calendar.Dtos;
using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.Sprints;
using GameFlow.Application.Features.Sprints.Dtos;
using GameFlow.Application.Features.Users;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Application.Features.WorkItems;
using GameFlow.Application.Features.WorkItems.Dtos;
using GameFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Dashboard;

/// <summary>Dashboard'daki tüm kartların verisini tek istekte döner.</summary>
public record DashboardDto(
    IReadOnlyList<WorkItemSummaryDto> TodayTasks,
    IReadOnlyList<WorkItemSummaryDto> UpcomingDeadlines,
    IReadOnlyList<WorkItemSummaryDto> OverdueTasks,
    int CompletionPercent,
    int TotalTaskCount,
    int CompletedTaskCount,
    int ActiveTaskCount,
    IReadOnlyList<ActivityDto> RecentActivities,
    IReadOnlyList<AnnouncementDto> Announcements,
    IReadOnlyList<UserSummaryDto> OnlineUsers,
    IReadOnlyList<SprintSummaryDto> ActiveSprints,
    IReadOnlyList<MeetingDto> UpcomingMeetings);

public class DashboardRequest
{
    /// <summary>Belirli bir projeye odaklanmak için. Boşsa kullanıcının tüm kapsamı.</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>Görev kartlarını yalnızca kullanıcıya atanmış görevlerle sınırla.</summary>
    public bool OnlyMyTasks { get; set; } = true;

    public int UpcomingDays { get; set; } = 7;
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(
        DashboardRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Dashboard birleştirici. Alt modüllerin hazır servislerini yeniden kullanır;
/// böylece deadline ve sprint mantığı iki yerde tekrarlanmaz.
/// </summary>
public class DashboardService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IWorkItemService workItemService,
    ISprintService sprintService,
    IMeetingService meetingService,
    IAnnouncementService announcementService,
    IDateTimeProvider dateTime) : IDashboardService
{
    private const int RecentActivityLimit = 15;
    private const int AnnouncementLimit = 5;
    private const int OnlineUserLimit = 20;
    private const int UpcomingMeetingLimit = 5;

    public async Task<DashboardDto> GetAsync(
        DashboardRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var deadlines = await workItemService.GetDeadlineOverviewAsync(
            request.ProjectId,
            teamId: null,
            request.UpcomingDays,
            request.OnlyMyTasks,
            cancellationToken);

        var counts = await GetTaskCountsAsync(request, userId, cancellationToken);

        var activeSprints = await sprintService.GetListAsync(
            new SprintListRequest { ProjectId = request.ProjectId, Status = SprintStatus.Active },
            cancellationToken);

        var upcomingMeetings = await meetingService.GetListAsync(
            new MeetingListRequest
            {
                ProjectId = request.ProjectId,
                OnlyUpcoming = true,
                OnlyMine = true
            },
            cancellationToken);

        var announcements = await announcementService.GetListAsync(
            new AnnouncementListRequest { ProjectId = request.ProjectId },
            cancellationToken);

        return new DashboardDto(
            deadlines.DueToday,
            deadlines.Upcoming,
            deadlines.Overdue,
            counts.CompletionPercent,
            counts.Total,
            counts.Completed,
            counts.Active,
            await GetRecentActivitiesAsync(request, userId, cancellationToken),
            announcements.Take(AnnouncementLimit).ToList(),
            await GetOnlineUsersAsync(userId, cancellationToken),
            activeSprints,
            upcomingMeetings.Take(UpcomingMeetingLimit).ToList());
    }

    /// <summary>Görev sayıları ve tamamlanma yüzdesi tek sorguda hesaplanır.</summary>
    private async Task<(int Total, int Completed, int Active, int CompletionPercent)> GetTaskCountsAsync(
        DashboardRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var query = context.WorkItems.AsNoTracking();

        if (!currentUser.IsAdmin || request.OnlyMyTasks)
        {
            var projectIds = await context.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync(cancellationToken);

            query = query.Where(w => projectIds.Contains(w.ProjectId));
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(w => w.ProjectId == request.ProjectId.Value);
        }

        if (request.OnlyMyTasks)
        {
            query = query.Where(w => w.AssigneeId == userId);
        }

        var stats = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Completed = group.Count(w => w.Status == WorkItemStatus.Done),
                Cancelled = group.Count(w => w.Status == WorkItemStatus.Cancelled)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stats is null)
        {
            return (0, 0, 0, 0);
        }

        // İptal edilen görevler tamamlanma yüzdesine dâhil edilmez.
        var countable = stats.Total - stats.Cancelled;
        var active = countable - stats.Completed;
        var percent = countable == 0 ? 0 : stats.Completed * 100 / countable;

        return (stats.Total, stats.Completed, active, percent);
    }

    private async Task<IReadOnlyList<ActivityDto>> GetRecentActivitiesAsync(
        DashboardRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var query = context.ActivityLogs.AsNoTracking();

        if (!currentUser.IsAdmin)
        {
            var projectIds = await context.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync(cancellationToken);

            // Proje bağı olmayan kayıtlar (giriş, kullanıcı yönetimi) yalnızca yöneticiye gösterilir.
            query = query.Where(l => l.ProjectId != null && projectIds.Contains(l.ProjectId.Value));
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(l => l.ProjectId == request.ProjectId.Value);
        }

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(RecentActivityLimit)
            .Select(l => new ActivityDto(
                l.Id,
                l.Type,
                l.Description,
                l.Actor == null
                    ? null
                    : new UserSummaryDto(
                        l.Actor.Id,
                        l.Actor.FullName,
                        l.Actor.Email,
                        l.Actor.JobTitle,
                        l.Actor.AvatarUrl,
                        (SystemRole)l.Actor.RoleId,
                        l.Actor.IsOnline,
                        l.Actor.LastSeenAt),
                l.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Çevrimiçi kullanıcılar. Bayrak SignalR bağlantı olaylarıyla güncellenir;
    /// beklenmedik kapanmalarda takılı kalmaması için son görülme zamanı da denetlenir.
    /// </summary>
    private async Task<IReadOnlyList<UserSummaryDto>> GetOnlineUsersAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var staleThreshold = dateTime.UtcNow.AddMinutes(-5);

        return await context.Users
            .AsNoTracking()
            .Where(u => u.IsOnline
                        && u.IsActive
                        && u.Id != userId
                        && u.LastSeenAt != null
                        && u.LastSeenAt > staleThreshold)
            .OrderBy(u => u.FullName)
            .Take(OnlineUserLimit)
            .Select(UserProjections.ToSummary)
            .ToListAsync(cancellationToken);
    }
}
