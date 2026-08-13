using System.Linq.Expressions;
using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Calendar.Dtos;
using GameFlow.Application.Features.Notifications.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Calendar;

public interface IMeetingService
{
    Task<IReadOnlyList<MeetingDto>> GetListAsync(
        MeetingListRequest request,
        CancellationToken cancellationToken = default);

    Task<MeetingDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MeetingDto> CreateAsync(
        CreateMeetingRequest request,
        CancellationToken cancellationToken = default);

    Task<MeetingDto> UpdateAsync(
        Guid id,
        UpdateMeetingRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Katılımcının toplantıya katılıp katılmayacağını bildirmesi.</summary>
    Task<MeetingDto> RespondAsync(
        Guid id,
        RespondToMeetingRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Toplantı yönetimi. Toplantıyı düzenleyen kişi, takım lideri, proje yöneticisi
/// veya sistem yöneticisi düzenleyebilir; katılımcılar yalnızca yanıt verebilir.
/// </summary>
public class MeetingService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IPermissionService permissions,
    INotificationService notifications,
    IActivityLogger activityLogger,
    IDateTimeProvider dateTime) : IMeetingService
{
    public async Task<IReadOnlyList<MeetingDto>> GetListAsync(
        MeetingListRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var query = context.Meetings.AsNoTracking();

        // Yönetici olmayan kullanıcılar yalnızca kendilerini ilgilendiren toplantıları görür.
        if (!currentUser.IsAdmin)
        {
            var projectIds = await context.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync(cancellationToken);

            var teamIds = await context.TeamMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.TeamId)
                .ToListAsync(cancellationToken);

            query = query.Where(m =>
                m.OrganizerId == userId
                || m.Attendees.Any(a => a.UserId == userId)
                || m.ProjectId != null && projectIds.Contains(m.ProjectId.Value)
                || m.TeamId != null && teamIds.Contains(m.TeamId.Value));
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(m => m.ProjectId == request.ProjectId.Value);
        }

        if (request.TeamId.HasValue)
        {
            query = query.Where(m => m.TeamId == request.TeamId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }

        if (request.OnlyUpcoming)
        {
            var now = dateTime.UtcNow;
            query = query.Where(m => m.EndsAt >= now && m.Status != MeetingStatus.Cancelled);
        }

        if (request.OnlyMine)
        {
            query = query.Where(m =>
                m.OrganizerId == userId || m.Attendees.Any(a => a.UserId == userId));
        }

        return await query
            .OrderBy(m => m.StartsAt)
            .Select(Projection(userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<MeetingDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var meeting = await context.Meetings
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(Projection(userId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Toplantı", id);

        await EnsureCanViewAsync(meeting, userId, cancellationToken);

        return meeting;
    }

    public async Task<MeetingDto> CreateAsync(
        CreateMeetingRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanOrganizeAsync(request.ProjectId, request.TeamId, cancellationToken);

        var organizerId = currentUser.RequireUserId();
        var attendeeIds = await ResolveAttendeesAsync(request, organizerId, cancellationToken);

        var meeting = new Meeting
        {
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Location = Normalize(request.Location),
            MeetingUrl = Normalize(request.MeetingUrl),
            ProjectId = request.ProjectId,
            TeamId = request.TeamId,
            OrganizerId = organizerId,
            Status = MeetingStatus.Scheduled
        };

        context.Meetings.Add(meeting);

        foreach (var attendeeId in attendeeIds)
        {
            context.MeetingAttendees.Add(new MeetingAttendee
            {
                MeetingId = meeting.Id,
                UserId = attendeeId,
                // Düzenleyen kişi katılımı otomatik onaylanmış sayılır.
                IsAccepted = attendeeId == organizerId ? true : null,
                RespondedAt = attendeeId == organizerId ? dateTime.UtcNow : null
            });
        }

        activityLogger.Log(
            ActivityType.MeetingCreated,
            $"\"{meeting.Title}\" toplantısı oluşturuldu.",
            projectId: meeting.ProjectId,
            teamId: meeting.TeamId,
            entityType: nameof(Meeting),
            entityId: meeting.Id);

        notifications.QueueMany(attendeeIds.Select(attendeeId => new NotificationRequest(
            attendeeId,
            NotificationType.MeetingCreated,
            "Yeni toplantı",
            $"{meeting.Title} · {meeting.StartsAt:dd.MM.yyyy HH:mm}",
            $"/toplantilar/{meeting.Id}",
            nameof(Meeting),
            meeting.Id)));

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetByIdAsync(meeting.Id, cancellationToken);
    }

    public async Task<MeetingDto> UpdateAsync(
        Guid id,
        UpdateMeetingRequest request,
        CancellationToken cancellationToken = default)
    {
        var meeting = await context.Meetings
            .Include(m => m.Attendees)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException("Toplantı", id);

        await EnsureCanModifyAsync(meeting, cancellationToken);
        await EnsureCanOrganizeAsync(request.ProjectId, request.TeamId, cancellationToken);

        var scheduleChanged = meeting.StartsAt != request.StartsAt || meeting.EndsAt != request.EndsAt;

        meeting.Title = request.Title.Trim();
        meeting.Description = Normalize(request.Description);
        meeting.StartsAt = request.StartsAt;
        meeting.EndsAt = request.EndsAt;
        meeting.Location = Normalize(request.Location);
        meeting.MeetingUrl = Normalize(request.MeetingUrl);
        meeting.ProjectId = request.ProjectId;
        meeting.TeamId = request.TeamId;
        meeting.Status = request.Status;

        var desiredIds = await ResolveAttendeesAsync(request, meeting.OrganizerId, cancellationToken);
        SyncAttendees(meeting, desiredIds);

        // Saat değiştiyse katılımcılar yeniden bilgilendirilir.
        if (scheduleChanged)
        {
            notifications.QueueMany(desiredIds.Select(attendeeId => new NotificationRequest(
                attendeeId,
                NotificationType.MeetingCreated,
                "Toplantı saati değişti",
                $"{meeting.Title} · {meeting.StartsAt:dd.MM.yyyy HH:mm}",
                $"/toplantilar/{meeting.Id}",
                nameof(Meeting),
                meeting.Id)));
        }

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetByIdAsync(meeting.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var meeting = await context.Meetings
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException("Toplantı", id);

        await EnsureCanModifyAsync(meeting, cancellationToken);

        context.Meetings.Remove(meeting);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeetingDto> RespondAsync(
        Guid id,
        RespondToMeetingRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var attendee = await context.MeetingAttendees
            .FirstOrDefaultAsync(a => a.MeetingId == id && a.UserId == userId, cancellationToken)
            ?? throw new ForbiddenException("Bu toplantının katılımcısı değilsiniz.");

        attendee.IsAccepted = request.IsAccepted;
        attendee.RespondedAt = dateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    // ------------------------------------------------------------- Yardımcılar

    /// <summary>
    /// Katılımcı listesini çözer: takım veya proje seçilmişse ve liste boşsa
    /// o grubun tüm üyeleri katılımcı yapılır. Düzenleyen her zaman eklenir.
    /// </summary>
    private async Task<List<Guid>> ResolveAttendeesAsync(
        CreateMeetingRequest request,
        Guid organizerId,
        CancellationToken cancellationToken)
    {
        var attendeeIds = request.AttendeeIds.Distinct().ToHashSet();

        if (attendeeIds.Count == 0)
        {
            if (request.TeamId.HasValue)
            {
                var teamMemberIds = await context.TeamMembers
                    .Where(m => m.TeamId == request.TeamId.Value)
                    .Select(m => m.UserId)
                    .ToListAsync(cancellationToken);

                attendeeIds.UnionWith(teamMemberIds);
            }
            else if (request.ProjectId.HasValue)
            {
                var projectMemberIds = await context.ProjectMembers
                    .Where(m => m.ProjectId == request.ProjectId.Value)
                    .Select(m => m.UserId)
                    .ToListAsync(cancellationToken);

                attendeeIds.UnionWith(projectMemberIds);
            }
        }
        else
        {
            var activeCount = await context.Users
                .CountAsync(u => attendeeIds.Contains(u.Id) && u.IsActive, cancellationToken);

            if (activeCount != attendeeIds.Count)
            {
                throw new DomainException("Katılımcılardan biri bulunamadı veya devre dışı.");
            }
        }

        attendeeIds.Add(organizerId);

        return attendeeIds.ToList();
    }

    private static void SyncAttendees(Meeting meeting, IReadOnlyCollection<Guid> desiredIds)
    {
        var desired = desiredIds.ToHashSet();

        foreach (var removed in meeting.Attendees.Where(a => !desired.Contains(a.UserId)).ToList())
        {
            meeting.Attendees.Remove(removed);
        }

        var current = meeting.Attendees.Select(a => a.UserId).ToHashSet();

        foreach (var added in desired.Except(current))
        {
            meeting.Attendees.Add(new MeetingAttendee { MeetingId = meeting.Id, UserId = added });
        }
    }

    private async Task EnsureCanViewAsync(
        MeetingDto meeting,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsAdmin
            || meeting.Organizer.Id == userId
            || meeting.Attendees.Any(a => a.User.Id == userId))
        {
            return;
        }

        if (meeting.ProjectId.HasValue
            && await permissions.IsProjectMemberAsync(meeting.ProjectId.Value, cancellationToken))
        {
            return;
        }

        if (meeting.TeamId.HasValue
            && await permissions.IsTeamMemberAsync(meeting.TeamId.Value, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException("Bu toplantıyı görme yetkiniz bulunmuyor.");
    }

    /// <summary>Toplantı oluşturma: takım lideri, proje yöneticisi veya yönetici.</summary>
    private async Task EnsureCanOrganizeAsync(
        Guid? projectId,
        Guid? teamId,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsAdmin)
        {
            return;
        }

        if (teamId.HasValue)
        {
            await permissions.EnsureCanManageTeamAsync(teamId.Value, cancellationToken);
            return;
        }

        if (projectId.HasValue)
        {
            await permissions.EnsureCanManageProjectAsync(projectId.Value, cancellationToken);
            return;
        }

        // Kapsam belirtilmemişse en az bir takımın lideri olmak gerekir.
        if (!await permissions.IsAnyTeamLeaderAsync(cancellationToken))
        {
            throw new ForbiddenException(
                "Toplantı oluşturmak için takım lideri veya yönetici olmalısınız.");
        }
    }

    private async Task EnsureCanModifyAsync(Meeting meeting, CancellationToken cancellationToken)
    {
        if (currentUser.IsAdmin || meeting.OrganizerId == currentUser.UserId)
        {
            return;
        }

        if (meeting.TeamId.HasValue
            && await permissions.CanManageTeamAsync(meeting.TeamId.Value, cancellationToken))
        {
            return;
        }

        if (meeting.ProjectId.HasValue
            && await permissions.CanManageProjectAsync(meeting.ProjectId.Value, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException("Bu toplantıyı değiştirme yetkiniz bulunmuyor.");
    }

    private static Expression<Func<Meeting, MeetingDto>> Projection(Guid userId)
        => meeting => new MeetingDto(
            meeting.Id,
            meeting.Title,
            meeting.Description,
            meeting.StartsAt,
            meeting.EndsAt,
            meeting.Location,
            meeting.MeetingUrl,
            meeting.Status,
            new UserSummaryDto(
                meeting.Organizer.Id,
                meeting.Organizer.FullName,
                meeting.Organizer.Email,
                meeting.Organizer.JobTitle,
                meeting.Organizer.AvatarUrl,
                (SystemRole)meeting.Organizer.RoleId,
                meeting.Organizer.IsOnline,
                meeting.Organizer.LastSeenAt),
            meeting.ProjectId,
            meeting.Project == null ? null : meeting.Project.Name,
            meeting.TeamId,
            meeting.Team == null ? null : meeting.Team.Name,
            meeting.Attendees
                .OrderBy(a => a.User.FullName)
                .Select(a => new MeetingAttendeeDto(
                    new UserSummaryDto(
                        a.User.Id,
                        a.User.FullName,
                        a.User.Email,
                        a.User.JobTitle,
                        a.User.AvatarUrl,
                        (SystemRole)a.User.RoleId,
                        a.User.IsOnline,
                        a.User.LastSeenAt),
                    a.IsAccepted,
                    a.RespondedAt))
                .ToList(),
            meeting.Attendees
                .Where(a => a.UserId == userId)
                .Select(a => a.IsAccepted)
                .FirstOrDefault());

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
