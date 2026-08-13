using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Common.Models;
using GameFlow.Application.Features.Notifications.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Notifications;

/// <inheritdoc cref="INotificationQueryService"/>
public class NotificationQueryService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IRealtimeNotifier realtimeNotifier,
    IDateTimeProvider dateTime) : INotificationQueryService
{
    public async Task<PagedResult<NotificationDto>> GetListAsync(
        NotificationListRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        // Kullanıcı yalnızca kendi bildirimlerini görebilir.
        var query = context.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (request.OnlyUnread)
        {
            query = query.Where(n => !n.IsRead);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.Link,
                n.IsRead,
                n.CreatedAt,
                n.Actor == null
                    ? null
                    : new UserSummaryDto(
                        n.Actor.Id,
                        n.Actor.FullName,
                        n.Actor.Email,
                        n.Actor.JobTitle,
                        n.Actor.AvatarUrl,
                        (SystemRole)n.Actor.RoleId,
                        n.Actor.IsOnline,
                        n.Actor.LastSeenAt)))
            .ToListAsync(cancellationToken);

        return PagedResult<NotificationDto>.Create(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Bildirim", id);

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = dateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await PushUnreadCountAsync(userId, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var now = dateTime.UtcNow;

        // Toplu güncelleme tek sorguda yapılır; bildirim sayısı yüksek olabilir.
        await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now),
                cancellationToken);

        await realtimeNotifier.SendUnreadCountAsync(userId, 0, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Bildirim", id);

        context.Notifications.Remove(notification);

        await context.SaveChangesAsync(cancellationToken);
        await PushUnreadCountAsync(userId, cancellationToken);
    }

    private async Task PushUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var unreadCount = await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        await realtimeNotifier.SendUnreadCountAsync(userId, unreadCount, cancellationToken);
    }
}
