using System.Linq.Expressions;
using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Announcements.Dtos;
using GameFlow.Application.Features.Notifications.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Announcements;

public interface IAnnouncementService
{
    Task<IReadOnlyList<AnnouncementDto>> GetListAsync(
        AnnouncementListRequest request,
        CancellationToken cancellationToken = default);

    Task<AnnouncementDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AnnouncementDto> CreateAsync(
        CreateAnnouncementRequest request,
        CancellationToken cancellationToken = default);

    Task<AnnouncementDto> UpdateAsync(
        Guid id,
        UpdateAnnouncementRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Duyurular. Yalnızca yöneticiler duyuru yayınlar; tüm kullanıcılar okur.
/// Proje duyuruları yalnızca o projenin üyelerine gösterilir.
/// </summary>
public class AnnouncementService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    INotificationService notifications,
    IActivityLogger activityLogger,
    IDateTimeProvider dateTime) : IAnnouncementService
{
    public async Task<IReadOnlyList<AnnouncementDto>> GetListAsync(
        AnnouncementListRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var now = dateTime.UtcNow;

        var query = context.Announcements.AsNoTracking();

        if (!request.IncludeExpired)
        {
            query = query.Where(a => a.ExpiresAt == null || a.ExpiresAt > now);
        }

        if (request.ProjectId.HasValue)
        {
            // Proje ekranında hem o projenin duyuruları hem stüdyo geneli duyurular
            // gösterilir; aksi halde herkesi ilgilendiren duyurular gizli kalırdı.
            query = query.Where(a => a.ProjectId == request.ProjectId.Value || a.ProjectId == null);
        }
        else if (!currentUser.IsAdmin)
        {
            // Stüdyo geneli duyurular herkese, proje duyuruları yalnızca üyelerine.
            var projectIds = await context.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync(cancellationToken);

            query = query.Where(a => a.ProjectId == null || projectIds.Contains(a.ProjectId.Value));
        }

        return await query
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAt)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }

    public async Task<AnnouncementDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await context.Announcements
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Duyuru", id);

    public async Task<AnnouncementDto> CreateAsync(
        CreateAnnouncementRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorId = currentUser.RequireUserId();

        if (request.ProjectId.HasValue
            && !await context.Projects.AnyAsync(p => p.Id == request.ProjectId.Value, cancellationToken))
        {
            throw new NotFoundException("Proje", request.ProjectId.Value);
        }

        var announcement = new Announcement
        {
            AuthorId = authorId,
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Priority = request.Priority,
            IsPinned = request.IsPinned,
            ExpiresAt = request.ExpiresAt,
            PublishedAt = dateTime.UtcNow
        };

        context.Announcements.Add(announcement);

        activityLogger.Log(
            ActivityType.AnnouncementPublished,
            $"\"{announcement.Title}\" duyurusu yayınlandı.",
            projectId: announcement.ProjectId,
            entityType: nameof(Announcement),
            entityId: announcement.Id);

        await QueueNotificationsAsync(announcement, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return await GetByIdAsync(announcement.Id, cancellationToken);
    }

    public async Task<AnnouncementDto> UpdateAsync(
        Guid id,
        UpdateAnnouncementRequest request,
        CancellationToken cancellationToken = default)
    {
        var announcement = await context.Announcements
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Duyuru", id);

        announcement.Title = request.Title.Trim();
        announcement.Content = request.Content.Trim();
        announcement.Priority = request.Priority;
        announcement.IsPinned = request.IsPinned;
        announcement.ProjectId = request.ProjectId;
        announcement.ExpiresAt = request.ExpiresAt;

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(announcement.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var announcement = await context.Announcements
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Duyuru", id);

        context.Announcements.Remove(announcement);

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Duyuru kapsamındaki tüm kullanıcılara bildirim gönderir.</summary>
    private async Task QueueNotificationsAsync(
        Announcement announcement,
        CancellationToken cancellationToken)
    {
        var recipientIds = announcement.ProjectId.HasValue
            ? await context.ProjectMembers
                .Where(m => m.ProjectId == announcement.ProjectId.Value)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken)
            : await context.Users
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

        notifications.QueueMany(recipientIds.Select(recipientId => new NotificationRequest(
            recipientId,
            NotificationType.AnnouncementPublished,
            "Yeni duyuru",
            announcement.Title,
            $"/duyurular/{announcement.Id}",
            nameof(Announcement),
            announcement.Id)));
    }

    private static readonly Expression<Func<Announcement, AnnouncementDto>> Projection =
        announcement => new AnnouncementDto(
            announcement.Id,
            announcement.Title,
            announcement.Content,
            announcement.Priority,
            announcement.IsPinned,
            announcement.PublishedAt,
            announcement.ExpiresAt,
            new UserSummaryDto(
                announcement.Author.Id,
                announcement.Author.FullName,
                announcement.Author.Email,
                announcement.Author.JobTitle,
                announcement.Author.AvatarUrl,
                (SystemRole)announcement.Author.RoleId,
                announcement.Author.IsOnline,
                announcement.Author.LastSeenAt),
            announcement.ProjectId,
            announcement.Project == null ? null : announcement.Project.Name);
}
