using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Calendar.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Calendar;

public interface ICalendarService
{
    /// <summary>
    /// Verilen tarih aralığındaki tüm takvim öğelerini döner: görev son tarihleri,
    /// sprint başlangıç/bitişleri, toplantılar ve elle eklenen etkinlikler.
    /// </summary>
    Task<IReadOnlyList<CalendarItemDto>> GetItemsAsync(
        CalendarRangeRequest request,
        CancellationToken cancellationToken = default);

    Task<CalendarItemDto> CreateEventAsync(
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken = default);

    Task<CalendarItemDto> UpdateEventAsync(
        Guid id,
        UpdateCalendarEventRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Takvim. Görev ve sprint tarihleri ayrı bir tabloda çoğaltılmaz; sorgu anında
/// kaynaklarından türetilir. Böylece bir görevin son tarihi değiştiğinde takvim
/// senkronizasyonu gerektirmez ve veri tekilliği korunur.
/// </summary>
public class CalendarService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IPermissionService permissions) : ICalendarService
{
    public async Task<IReadOnlyList<CalendarItemDto>> GetItemsAsync(
        CalendarRangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(request, cancellationToken);
        var wanted = request.Types.Count == 0 ? null : request.Types.ToHashSet();

        var items = new List<CalendarItemDto>();

        if (Includes(wanted, CalendarEventType.Deadline))
        {
            items.AddRange(await GetDeadlineItemsAsync(request, scope, cancellationToken));
        }

        if (Includes(wanted, CalendarEventType.SprintStart)
            || Includes(wanted, CalendarEventType.SprintEnd))
        {
            items.AddRange(await GetSprintItemsAsync(request, scope, wanted, cancellationToken));
        }

        if (Includes(wanted, CalendarEventType.Meeting))
        {
            items.AddRange(await GetMeetingItemsAsync(request, scope, cancellationToken));
        }

        items.AddRange(await GetCustomEventsAsync(request, scope, wanted, cancellationToken));

        return items.OrderBy(item => item.StartsAt).ThenBy(item => item.Title).ToList();
    }

    public async Task<CalendarItemDto> CreateEventAsync(
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureScopeAccessAsync(request.ProjectId, request.TeamId, cancellationToken);

        var calendarEvent = new CalendarEvent
        {
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            Type = request.Type,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            IsAllDay = request.IsAllDay,
            ColorHex = request.ColorHex,
            ProjectId = request.ProjectId,
            TeamId = request.TeamId,
            CreatedById = currentUser.RequireUserId()
        };

        context.CalendarEvents.Add(calendarEvent);

        await context.SaveChangesAsync(cancellationToken);

        return await GetEventAsync(calendarEvent.Id, cancellationToken);
    }

    public async Task<CalendarItemDto> UpdateEventAsync(
        Guid id,
        UpdateCalendarEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await context.CalendarEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException("Takvim etkinliği", id);

        await EnsureCanModifyEventAsync(calendarEvent, cancellationToken);
        await EnsureScopeAccessAsync(request.ProjectId, request.TeamId, cancellationToken);

        calendarEvent.Title = request.Title.Trim();
        calendarEvent.Description = Normalize(request.Description);
        calendarEvent.Type = request.Type;
        calendarEvent.StartsAt = request.StartsAt;
        calendarEvent.EndsAt = request.EndsAt;
        calendarEvent.IsAllDay = request.IsAllDay;
        calendarEvent.ColorHex = request.ColorHex;
        calendarEvent.ProjectId = request.ProjectId;
        calendarEvent.TeamId = request.TeamId;

        await context.SaveChangesAsync(cancellationToken);

        return await GetEventAsync(calendarEvent.Id, cancellationToken);
    }

    public async Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var calendarEvent = await context.CalendarEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException("Takvim etkinliği", id);

        await EnsureCanModifyEventAsync(calendarEvent, cancellationToken);

        context.CalendarEvents.Remove(calendarEvent);

        await context.SaveChangesAsync(cancellationToken);
    }

    // --------------------------------------------------------- Öğe kaynakları

    /// <summary>Görev son teslim tarihleri.</summary>
    private async Task<List<CalendarItemDto>> GetDeadlineItemsAsync(
        CalendarRangeRequest request,
        CalendarScope scope,
        CancellationToken cancellationToken)
    {
        var query = context.WorkItems
            .AsNoTracking()
            .Where(w =>
                w.DueDate != null
                && w.DueDate >= request.From
                && w.DueDate <= request.To
                && w.Status != WorkItemStatus.Cancelled);

        query = scope.IsAdminWithoutFilter
            ? query
            : query.Where(w => scope.ProjectIds.Contains(w.ProjectId));

        if (request.ProjectId.HasValue)
        {
            query = query.Where(w => w.ProjectId == request.ProjectId.Value);
        }

        if (request.TeamId.HasValue)
        {
            query = query.Where(w => w.TeamId == request.TeamId.Value);
        }

        if (request.OnlyMine)
        {
            query = query.Where(w => w.AssigneeId == scope.UserId);
        }

        return await query
            .Select(w => new CalendarItemDto(
                w.Id,
                $"{w.Key} · {w.Title}",
                CalendarEventType.Deadline,
                w.DueDate!.Value,
                null,
                true,
                // Tamamlanan görev yeşil, geciken kırmızı, diğerleri turuncu.
                w.Status == WorkItemStatus.Done
                    ? "#22C55E"
                    : w.DueDate < DateTime.UtcNow
                        ? "#EF4444"
                        : "#F97316",
                "/gorevler/" + w.Key,
                w.ProjectId,
                w.Project.Name,
                w.TeamId,
                w.Team == null ? null : w.Team.Name))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Sprint başlangıç ve bitiş tarihleri.</summary>
    private async Task<List<CalendarItemDto>> GetSprintItemsAsync(
        CalendarRangeRequest request,
        CalendarScope scope,
        HashSet<CalendarEventType>? wanted,
        CancellationToken cancellationToken)
    {
        var query = context.Sprints
            .AsNoTracking()
            .Where(s => s.Status != SprintStatus.Cancelled
                        && (s.StartDate >= request.From && s.StartDate <= request.To
                            || s.EndDate >= request.From && s.EndDate <= request.To));

        query = scope.IsAdminWithoutFilter
            ? query
            : query.Where(s => scope.ProjectIds.Contains(s.ProjectId));

        if (request.ProjectId.HasValue)
        {
            query = query.Where(s => s.ProjectId == request.ProjectId.Value);
        }

        if (request.TeamId.HasValue)
        {
            query = query.Where(s => s.TeamId == request.TeamId.Value);
        }

        var sprints = await query
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.StartDate,
                s.EndDate,
                s.ProjectId,
                ProjectName = s.Project.Name,
                s.TeamId,
                TeamName = s.Team == null ? null : s.Team.Name
            })
            .ToListAsync(cancellationToken);

        var items = new List<CalendarItemDto>();

        foreach (var sprint in sprints)
        {
            if (Includes(wanted, CalendarEventType.SprintStart)
                && sprint.StartDate >= request.From
                && sprint.StartDate <= request.To)
            {
                items.Add(new CalendarItemDto(
                    sprint.Id,
                    $"{sprint.Name} başlangıcı",
                    CalendarEventType.SprintStart,
                    sprint.StartDate,
                    null,
                    true,
                    "#8B5CF6",
                    $"/sprintler/{sprint.Id}",
                    sprint.ProjectId,
                    sprint.ProjectName,
                    sprint.TeamId,
                    sprint.TeamName));
            }

            if (Includes(wanted, CalendarEventType.SprintEnd)
                && sprint.EndDate >= request.From
                && sprint.EndDate <= request.To)
            {
                items.Add(new CalendarItemDto(
                    sprint.Id,
                    $"{sprint.Name} bitişi",
                    CalendarEventType.SprintEnd,
                    sprint.EndDate,
                    null,
                    true,
                    "#6366F1",
                    $"/sprintler/{sprint.Id}",
                    sprint.ProjectId,
                    sprint.ProjectName,
                    sprint.TeamId,
                    sprint.TeamName));
            }
        }

        return items;
    }

    private async Task<List<CalendarItemDto>> GetMeetingItemsAsync(
        CalendarRangeRequest request,
        CalendarScope scope,
        CancellationToken cancellationToken)
    {
        var query = context.Meetings
            .AsNoTracking()
            .Where(m => m.StartsAt >= request.From
                        && m.StartsAt <= request.To
                        && m.Status != MeetingStatus.Cancelled);

        // Kullanıcı; katılımcısı olduğu, düzenlediği veya kapsamındaki toplantıları görür.
        if (!scope.IsAdminWithoutFilter)
        {
            query = query.Where(m =>
                m.OrganizerId == scope.UserId
                || m.Attendees.Any(a => a.UserId == scope.UserId)
                || m.ProjectId != null && scope.ProjectIds.Contains(m.ProjectId.Value)
                || m.TeamId != null && scope.TeamIds.Contains(m.TeamId.Value));
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(m => m.ProjectId == request.ProjectId.Value);
        }

        if (request.TeamId.HasValue)
        {
            query = query.Where(m => m.TeamId == request.TeamId.Value);
        }

        if (request.OnlyMine)
        {
            query = query.Where(m =>
                m.OrganizerId == scope.UserId || m.Attendees.Any(a => a.UserId == scope.UserId));
        }

        return await query
            .Select(m => new CalendarItemDto(
                m.Id,
                m.Title,
                CalendarEventType.Meeting,
                m.StartsAt,
                m.EndsAt,
                false,
                "#3B82F6",
                $"/toplantilar/{m.Id}",
                m.ProjectId,
                m.Project == null ? null : m.Project.Name,
                m.TeamId,
                m.Team == null ? null : m.Team.Name))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<CalendarItemDto>> GetCustomEventsAsync(
        CalendarRangeRequest request,
        CalendarScope scope,
        HashSet<CalendarEventType>? wanted,
        CancellationToken cancellationToken)
    {
        var query = context.CalendarEvents
            .AsNoTracking()
            .Where(e => e.StartsAt >= request.From && e.StartsAt <= request.To);

        // Elle eklenen etkinlikler için de tür filtresi uygulanır.
        if (wanted is not null)
        {
            query = query.Where(e => wanted.Contains(e.Type));
        }

        if (!scope.IsAdminWithoutFilter)
        {
            query = query.Where(e =>
                e.CreatedById == scope.UserId
                || e.ProjectId == null && e.TeamId == null
                || e.ProjectId != null && scope.ProjectIds.Contains(e.ProjectId.Value)
                || e.TeamId != null && scope.TeamIds.Contains(e.TeamId.Value));
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(e => e.ProjectId == request.ProjectId.Value);
        }

        if (request.TeamId.HasValue)
        {
            query = query.Where(e => e.TeamId == request.TeamId.Value);
        }

        if (request.OnlyMine)
        {
            query = query.Where(e => e.CreatedById == scope.UserId);
        }

        return await query.Select(EventProjection).ToListAsync(cancellationToken);
    }

    // ------------------------------------------------------------- Yardımcılar

    private static readonly System.Linq.Expressions.Expression<Func<CalendarEvent, CalendarItemDto>>
        EventProjection = e => new CalendarItemDto(
            e.Id,
            e.Title,
            e.Type,
            e.StartsAt,
            e.EndsAt,
            e.IsAllDay,
            e.ColorHex,
            null,
            e.ProjectId,
            e.Project == null ? null : e.Project.Name,
            e.TeamId,
            e.Team == null ? null : e.Team.Name);

    private async Task<CalendarItemDto> GetEventAsync(Guid id, CancellationToken cancellationToken)
        => await context.CalendarEvents
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(EventProjection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Takvim etkinliği", id);

    private static bool Includes(HashSet<CalendarEventType>? wanted, CalendarEventType type)
        => wanted is null || wanted.Contains(type);

    /// <summary>Kullanıcının erişebildiği proje ve takım kimlikleri.</summary>
    private sealed record CalendarScope(
        Guid UserId,
        List<Guid> ProjectIds,
        List<Guid> TeamIds,
        bool IsAdminWithoutFilter);

    private async Task<CalendarScope> ResolveScopeAsync(
        CalendarRangeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        if (request.ProjectId.HasValue)
        {
            await permissions.EnsureProjectMemberAsync(request.ProjectId.Value, cancellationToken);
        }

        if (request.TeamId.HasValue)
        {
            await permissions.EnsureTeamMemberAsync(request.TeamId.Value, cancellationToken);
        }

        // Yönetici, "yalnızca benim" filtresi yoksa tüm kayıtları görebilir.
        if (currentUser.IsAdmin && !request.OnlyMine)
        {
            return new CalendarScope(userId, [], [], true);
        }

        var projectIds = await context.ProjectMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.ProjectId)
            .ToListAsync(cancellationToken);

        var teamIds = await context.TeamMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.TeamId)
            .ToListAsync(cancellationToken);

        return new CalendarScope(userId, projectIds, teamIds, false);
    }

    private async Task EnsureScopeAccessAsync(
        Guid? projectId,
        Guid? teamId,
        CancellationToken cancellationToken)
    {
        if (projectId.HasValue)
        {
            await permissions.EnsureProjectMemberAsync(projectId.Value, cancellationToken);
        }

        if (teamId.HasValue)
        {
            await permissions.EnsureTeamMemberAsync(teamId.Value, cancellationToken);
        }
    }

    /// <summary>Etkinliği oluşturan kişi, takım lideri, proje yöneticisi veya yönetici değiştirebilir.</summary>
    private async Task EnsureCanModifyEventAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsAdmin || calendarEvent.CreatedById == currentUser.UserId)
        {
            return;
        }

        if (calendarEvent.TeamId.HasValue
            && await permissions.CanManageTeamAsync(calendarEvent.TeamId.Value, cancellationToken))
        {
            return;
        }

        if (calendarEvent.ProjectId.HasValue
            && await permissions.CanManageProjectAsync(calendarEvent.ProjectId.Value, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException("Bu etkinliği değiştirme yetkiniz bulunmuyor.");
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
