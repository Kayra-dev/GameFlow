using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Notifications.Dtos;

namespace GameFlow.Infrastructure.Services;

/// <summary>
/// Anlık iletim devrede değilken kullanılan etkisiz uygulama.
/// API katmanı SignalR hub'ını kaydettiğinde bu kayıt ezilir; böylece
/// uygulama katmanı her iki durumda da aynı şekilde çalışır.
/// </summary>
public class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task SendNotificationAsync(
        Guid userId,
        NotificationDto notification,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendUnreadCountAsync(
        Guid userId,
        int unreadCount,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendWorkItemChangedAsync(
        Guid projectId,
        Guid workItemId,
        string changeKind,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
