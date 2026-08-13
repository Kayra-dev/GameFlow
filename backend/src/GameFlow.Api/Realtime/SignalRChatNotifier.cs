using GameFlow.Api.Hubs;
using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Chat.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace GameFlow.Api.Realtime;

/// <summary><see cref="IChatNotifier"/>'ın SignalR uygulaması.</summary>
public class SignalRChatNotifier(IHubContext<ChatHub, IChatClient> hubContext) : IChatNotifier
{
    public Task MessageReceivedAsync(
        Guid roomId,
        MessageDto message,
        CancellationToken cancellationToken = default)
        => hubContext.Clients.Group(HubGroups.ChatRoom(roomId)).MessageReceived(message);

    public Task MessageEditedAsync(
        Guid roomId,
        MessageDto message,
        CancellationToken cancellationToken = default)
        => hubContext.Clients.Group(HubGroups.ChatRoom(roomId)).MessageEdited(message);

    public Task MessageDeletedAsync(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken = default)
        => hubContext.Clients.Group(HubGroups.ChatRoom(roomId)).MessageDeleted(roomId, messageId);

    public Task MessagesReadAsync(
        Guid roomId,
        Guid userId,
        IReadOnlyCollection<Guid> messageIds,
        CancellationToken cancellationToken = default)
        => hubContext.Clients.Group(HubGroups.ChatRoom(roomId)).MessagesRead(roomId, userId, messageIds);
}
