using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.WorkItems;
using GameFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Reports;

/// <summary>Grafiklerde kullanılan tek veri noktası.</summary>
public record ReportSeriesPoint(string Label, int Value, string? ColorHex = null);

public record TeamPerformanceRow(
    Guid TeamId,
    string TeamName,
    string ColorHex,
    int CompletedTaskCount,
    int ActiveTaskCount,
    int OverdueTaskCount,
    int CompletionPercent,
    int MemberCount);

public record UserPerformanceRow(
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    int CompletedTaskCount,
    int ActiveTaskCount,
    int OverdueTaskCount,
    int StoryPoints);

public record ReportsDto(
    IReadOnlyList<TeamPerformanceRow> TeamPerformance,
    IReadOnlyList<UserPerformanceRow> UserPerformance,
    IReadOnlyList<ReportSeriesPoint> StatusDistribution,
    IReadOnlyList<ReportSeriesPoint> PriorityDistribution,
    IReadOnlyList<ReportSeriesPoint> TypeDistribution,
    /// <summary>Son 12 haftanın haftalık tamamlanan görev sayısı.</summary>
    IReadOnlyList<ReportSeriesPoint> WeeklyCompleted,
    /// <summary>Son 12 ayın aylık tamamlanan görev sayısı.</summary>
    IReadOnlyList<ReportSeriesPoint> MonthlyCompleted,
    /// <summary>Tamamlanmış sprintlerin başarı yüzdeleri.</summary>
    IReadOnlyList<ReportSeriesPoint> SprintSuccess,
    int TotalTaskCount,
    int CompletedTaskCount,
    int OverdueTaskCount,
    int CompletionPercent);

public class ReportRequest
{
    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }
}

public interface IReportService
{
    Task<ReportsDto> GetAsync(ReportRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Raporlama. Tüm toplamalar veritabanında yapılır; görev satırları uygulamaya
/// taşınmaz. Kapsam kullanıcının erişebildiği projelerle sınırlıdır.
/// </summary>
public class ReportService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IPermissionService permissions,
    IDateTimeProvider dateTime) : IReportService
{
    private const int WeeklyBuckets = 12;
    private const int MonthlyBuckets = 12;
    private const int UserRowLimit = 20;
    private const int SprintPointLimit = 10;

    public async Task<ReportsDto> GetAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProjectId.HasValue)
        {
            await permissions.EnsureProjectMemberAsync(request.ProjectId.Value, cancellationToken);
        }

        if (request.TeamId.HasValue)
        {
            await permissions.EnsureTeamMemberAsync(request.TeamId.Value, cancellationToken);
        }

        var now = dateTime.UtcNow;
        var scopedTasks = await BuildScopedQueryAsync(request, cancellationToken);

        var totals = await scopedTasks
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
                    && w.Status != WorkItemStatus.Cancelled)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var countable = (totals?.Total ?? 0) - (totals?.Cancelled ?? 0);
        var completionPercent = countable == 0 ? 0 : (totals?.Completed ?? 0) * 100 / countable;

        return new ReportsDto(
            await GetTeamPerformanceAsync(request, now, cancellationToken),
            await GetUserPerformanceAsync(scopedTasks, now, cancellationToken),
            await GetStatusDistributionAsync(scopedTasks, cancellationToken),
            await GetPriorityDistributionAsync(scopedTasks, cancellationToken),
            await GetTypeDistributionAsync(scopedTasks, cancellationToken),
            await GetWeeklyCompletedAsync(scopedTasks, now, cancellationToken),
            await GetMonthlyCompletedAsync(scopedTasks, now, cancellationToken),
            await GetSprintSuccessAsync(request, cancellationToken),
            totals?.Total ?? 0,
            totals?.Completed ?? 0,
            totals?.Overdue ?? 0,
            completionPercent);
    }

    private async Task<IQueryable<Domain.Entities.WorkItem>> BuildScopedQueryAsync(
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        var query = context.WorkItems.AsNoTracking();

        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();

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

        if (request.TeamId.HasValue)
        {
            query = query.Where(w => w.TeamId == request.TeamId.Value);
        }

        return query;
    }

    private async Task<IReadOnlyList<TeamPerformanceRow>> GetTeamPerformanceAsync(
        ReportRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var teams = context.Teams.AsNoTracking();

        if (request.TeamId.HasValue)
        {
            teams = teams.Where(t => t.Id == request.TeamId.Value);
        }

        var rows = await teams
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.ColorHex,
                MemberCount = t.Members.Count,
                Completed = t.WorkItems.Count(w =>
                    w.Status == WorkItemStatus.Done
                    && (request.ProjectId == null || w.ProjectId == request.ProjectId)),
                Active = t.WorkItems.Count(w =>
                    w.Status != WorkItemStatus.Done
                    && w.Status != WorkItemStatus.Cancelled
                    && (request.ProjectId == null || w.ProjectId == request.ProjectId)),
                Overdue = t.WorkItems.Count(w =>
                    w.DueDate != null
                    && w.DueDate < now
                    && w.Status != WorkItemStatus.Done
                    && w.Status != WorkItemStatus.Cancelled
                    && (request.ProjectId == null || w.ProjectId == request.ProjectId))
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row =>
            {
                var countable = row.Completed + row.Active;

                return new TeamPerformanceRow(
                    row.Id,
                    row.Name,
                    row.ColorHex,
                    row.Completed,
                    row.Active,
                    row.Overdue,
                    countable == 0 ? 0 : row.Completed * 100 / countable,
                    row.MemberCount);
            })
            .OrderByDescending(row => row.CompletedTaskCount)
            .ToList();
    }

    private static async Task<IReadOnlyList<UserPerformanceRow>> GetUserPerformanceAsync(
        IQueryable<Domain.Entities.WorkItem> scopedTasks,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // İki kısıt birlikte geçerli:
        // 1) Gruplama anahtarı varlığın kendisi olamaz; yalnızca skaler alanlar kullanılır.
        // 2) Projeksiyonda hesaplanan bir toplama göre OrderBy SQL'e çevrilemez.
        // Bu yüzden gruplar veritabanında toplanır, sıralama ve kesme bellekte yapılır.
        // Satır sayısı proje üyesi sayısı kadar olduğu için maliyeti önemsizdir.
        var rows = await scopedTasks
            .Where(w => w.AssigneeId != null)
            .GroupBy(w => new
            {
                UserId = w.Assignee!.Id,
                w.Assignee.FullName,
                w.Assignee.AvatarUrl
            })
            .Select(group => new UserPerformanceRow(
                group.Key.UserId,
                group.Key.FullName,
                group.Key.AvatarUrl,
                group.Count(w => w.Status == WorkItemStatus.Done),
                group.Count(w =>
                    w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Cancelled),
                group.Count(w =>
                    w.DueDate != null
                    && w.DueDate < now
                    && w.Status != WorkItemStatus.Done
                    && w.Status != WorkItemStatus.Cancelled),
                group.Sum(w => w.Status == WorkItemStatus.Done ? w.StoryPoints ?? 0 : 0)))
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.CompletedTaskCount)
            .ThenByDescending(row => row.StoryPoints)
            .Take(UserRowLimit)
            .ToList();
    }

    private static async Task<IReadOnlyList<ReportSeriesPoint>> GetStatusDistributionAsync(
        IQueryable<Domain.Entities.WorkItem> scopedTasks,
        CancellationToken cancellationToken)
    {
        var rows = await scopedTasks
            .GroupBy(w => w.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        // Kolon renkleri kanban ile tutarlı olacak şekilde sunucuda belirlenir.
        return WorkItemProjections.BoardColumnOrder
            .Select(status => new ReportSeriesPoint(
                WorkItemProjections.GetStatusLabel(status),
                rows.FirstOrDefault(row => row.Status == status)?.Count ?? 0,
                GetStatusColor(status)))
            .ToList();
    }

    private static async Task<IReadOnlyList<ReportSeriesPoint>> GetPriorityDistributionAsync(
        IQueryable<Domain.Entities.WorkItem> scopedTasks,
        CancellationToken cancellationToken)
    {
        var rows = await scopedTasks
            .GroupBy(w => w.Priority)
            .Select(group => new { Priority = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return Enum.GetValues<WorkItemPriority>()
            .Select(priority => new ReportSeriesPoint(
                WorkItemProjections.GetPriorityLabel(priority),
                rows.FirstOrDefault(row => row.Priority == priority)?.Count ?? 0,
                GetPriorityColor(priority)))
            .ToList();
    }

    private static async Task<IReadOnlyList<ReportSeriesPoint>> GetTypeDistributionAsync(
        IQueryable<Domain.Entities.WorkItem> scopedTasks,
        CancellationToken cancellationToken)
    {
        var rows = await scopedTasks
            .GroupBy(w => w.Type)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.Count)
            .Select(row => new ReportSeriesPoint(GetTypeLabel(row.Type), row.Count))
            .ToList();
    }

    /// <summary>Son 12 haftanın tamamlanan görev sayıları.</summary>
    private static async Task<IReadOnlyList<ReportSeriesPoint>> GetWeeklyCompletedAsync(
        IQueryable<Domain.Entities.WorkItem> scopedTasks,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var since = now.Date.AddDays(-7 * (WeeklyBuckets - 1));

        var completed = await scopedTasks
            .Where(w => w.CompletedAt != null && w.CompletedAt >= since)
            .Select(w => w.CompletedAt!.Value)
            .ToListAsync(cancellationToken);

        var points = new List<ReportSeriesPoint>(WeeklyBuckets);

        for (var index = WeeklyBuckets - 1; index >= 0; index--)
        {
            var weekStart = now.Date.AddDays(-7 * index);
            var weekEnd = weekStart.AddDays(7);

            points.Add(new ReportSeriesPoint(
                $"{weekStart:dd MMM}",
                completed.Count(date => date >= weekStart && date < weekEnd)));
        }

        return points;
    }

    private static async Task<IReadOnlyList<ReportSeriesPoint>> GetMonthlyCompletedAsync(
        IQueryable<Domain.Entities.WorkItem> scopedTasks,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var since = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(MonthlyBuckets - 1));

        var completed = await scopedTasks
            .Where(w => w.CompletedAt != null && w.CompletedAt >= since)
            .Select(w => w.CompletedAt!.Value)
            .ToListAsync(cancellationToken);

        var monthNames = new[]
        {
            "Oca", "Şub", "Mar", "Nis", "May", "Haz",
            "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"
        };

        var points = new List<ReportSeriesPoint>(MonthlyBuckets);

        for (var index = MonthlyBuckets - 1; index >= 0; index--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-index);
            var monthEnd = monthStart.AddMonths(1);

            points.Add(new ReportSeriesPoint(
                monthNames[monthStart.Month - 1],
                completed.Count(date => date >= monthStart && date < monthEnd)));
        }

        return points;
    }

    private async Task<IReadOnlyList<ReportSeriesPoint>> GetSprintSuccessAsync(
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        var query = context.Sprints
            .AsNoTracking()
            .Where(s => s.Status == SprintStatus.Completed);

        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();

            var projectIds = await context.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync(cancellationToken);

            query = query.Where(s => projectIds.Contains(s.ProjectId));
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(s => s.ProjectId == request.ProjectId.Value);
        }

        if (request.TeamId.HasValue)
        {
            query = query.Where(s => s.TeamId == request.TeamId.Value);
        }

        var sprints = await query
            .OrderByDescending(s => s.CompletedAt)
            .Take(SprintPointLimit)
            .Select(s => new
            {
                s.Name,
                Countable = s.WorkItems.Count(w => w.Status != WorkItemStatus.Cancelled),
                Completed = s.WorkItems.Count(w => w.Status == WorkItemStatus.Done)
            })
            .ToListAsync(cancellationToken);

        return sprints
            // Grafikte en eski sprint solda görünmeli.
            .AsEnumerable()
            .Reverse()
            .Select(sprint => new ReportSeriesPoint(
                sprint.Name,
                sprint.Countable == 0 ? 0 : sprint.Completed * 100 / sprint.Countable))
            .ToList();
    }

    private static string GetStatusColor(WorkItemStatus status) => status switch
    {
        WorkItemStatus.Pending => "#64748B",
        WorkItemStatus.Todo => "#3B82F6",
        WorkItemStatus.InProgress => "#8B5CF6",
        WorkItemStatus.CodeReview => "#F59E0B",
        WorkItemStatus.Testing => "#06B6D4",
        WorkItemStatus.Done => "#22C55E",
        WorkItemStatus.Cancelled => "#EF4444",
        _ => "#94A3B8"
    };

    private static string GetPriorityColor(WorkItemPriority priority) => priority switch
    {
        WorkItemPriority.Lowest => "#64748B",
        WorkItemPriority.Low => "#3B82F6",
        WorkItemPriority.Medium => "#F59E0B",
        WorkItemPriority.High => "#F97316",
        WorkItemPriority.Critical => "#EF4444",
        _ => "#94A3B8"
    };

    private static string GetTypeLabel(WorkItemType type) => type switch
    {
        WorkItemType.Feature => "Özellik",
        WorkItemType.Bug => "Hata",
        WorkItemType.Task => "Görev",
        WorkItemType.Improvement => "İyileştirme",
        WorkItemType.Research => "Araştırma",
        WorkItemType.ArtAsset => "Görsel Varlık",
        WorkItemType.AudioAsset => "Ses Varlığı",
        WorkItemType.LevelDesign => "Seviye Tasarımı",
        WorkItemType.Narrative => "Hikâye",
        WorkItemType.Playtest => "Oynanış Testi",
        _ => type.ToString()
    };
}
