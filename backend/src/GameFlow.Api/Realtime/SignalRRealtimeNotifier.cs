using GameFlow.Api.Hubs;
using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Notifications.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace GameFlow.Api.Realtime;

/// <summary>
/// <see cref="IRealtimeNotifier"/>'ın SignalR uygulaması.
/// Uygulama katmanı bu türü tanımaz; yalnızca arayüz üzerinden konuşur.
/// </summary>
public class SignalRRealtimeNotifier(IHubContext<PresenceHub, IPresenceClient> hubContext)
    : IRealtimeNotifier
{
    public Task SendNotificationAsync(
        Guid userId,
        NotificationDto notification,
        CancellationToken cancellationToken = default)
        => hubContext.Clients.Group(HubGroups.User(userId)).NotificationReceived(notification);

    public Task SendUnreadCountAsync(
        Guid userId,
        int unreadCount,
        CancellationToken cancellationToken = default)
        => hubContext.Clients.Group(HubGroups.User(userId)).UnreadCountChanged(unreadCount);

    public Task SendWorkItemChangedAsync(
        Guid projectId,
        Guid workItemId,
        string changeKind,
        CancellationToken cancellationToken = default)
        => hubContext.Clients
            .Group(HubGroups.Project(projectId))
            .WorkItemChanged(projectId, workItemId, changeKind);
}
