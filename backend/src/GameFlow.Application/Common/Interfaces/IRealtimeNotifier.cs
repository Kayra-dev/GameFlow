using GameFlow.Application.Features.Notifications.Dtos;

namespace GameFlow.Application.Common.Interfaces;

/// <summary>
/// Anlık iletim soyutlaması. Uygulama katmanı SignalR'ı tanımaz; gerçek
/// gönderim API katmanındaki hub uygulaması tarafından yapılır.
/// SignalR devrede değilse etkisiz (no-op) uygulama kullanılır.
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>Belirli bir kullanıcıya bildirim iletir.</summary>
    Task SendNotificationAsync(
        Guid userId,
        NotificationDto notification,
        CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının okunmamış bildirim sayısını güncelleyerek iletir.</summary>
    Task SendUnreadCountAsync(
        Guid userId,
        int unreadCount,
        CancellationToken cancellationToken = default);

    /// <summary>Bir projeye/panoya bağlı istemcilere görev değişikliğini bildirir.</summary>
    Task SendWorkItemChangedAsync(
        Guid projectId,
        Guid workItemId,
        string changeKind,
        CancellationToken cancellationToken = default);
}
