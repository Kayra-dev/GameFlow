using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Notifications.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Notifications;

/// <inheritdoc cref="INotificationService"/>
public class NotificationService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IRealtimeNotifier realtimeNotifier,
    IDateTimeProvider dateTime) : INotificationService
{
    private readonly List<Notification> _pending = [];

    public void Queue(NotificationRequest request)
    {
        var actorId = currentUser.UserId;

        // Kullanıcı kendi yaptığı işlem için bildirim almaz.
        if (actorId == request.RecipientId)
        {
            return;
        }

        var notification = new Notification
        {
            UserId = request.RecipientId,
            ActorId = actorId,
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            Link = request.Link,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            CreatedAt = dateTime.UtcNow
        };

        context.Notifications.Add(notification);
        _pending.Add(notification);
    }

    public void QueueMany(IEnumerable<NotificationRequest> requests)
    {
        foreach (var request in requests)
        {
            Queue(request);
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_pending.Count == 0)
        {
            return;
        }

        var actor = currentUser.UserId is { } actorId
            ? await context.Users
                .AsNoTracking()
                .Where(u => u.Id == actorId)
                .Select(UserProjectionExpression)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // Okunmamış sayıları tek sorguda alınır (alıcı başına ayrı sorgu yapılmaz).
        var recipientIds = _pending.Select(n => n.UserId).Distinct().ToList();

        var unreadCounts = await context.Notifications
            .AsNoTracking()
            .Where(n => recipientIds.Contains(n.UserId) && !n.IsRead)
            .GroupBy(n => n.UserId)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        foreach (var notification in _pending)
        {
            var dto = new NotificationDto(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.Link,
                notification.IsRead,
                notification.CreatedAt,
                actor);

            await realtimeNotifier.SendNotificationAsync(notification.UserId, dto, cancellationToken);
        }

        foreach (var recipientId in recipientIds)
        {
            await realtimeNotifier.SendUnreadCountAsync(
                recipientId,
                unreadCounts.GetValueOrDefault(recipientId),
                cancellationToken);
        }

        _pending.Clear();
    }

    private static readonly System.Linq.Expressions.Expression<Func<User, UserSummaryDto>>
        UserProjectionExpression = user => new UserSummaryDto(
            user.Id,
            user.FullName,
            user.Email,
            user.JobTitle,
            user.AvatarUrl,
            (SystemRole)user.RoleId,
            user.IsOnline,
            user.LastSeenAt);
}
