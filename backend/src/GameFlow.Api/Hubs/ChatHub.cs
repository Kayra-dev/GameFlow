using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Chat;
using GameFlow.Application.Features.Chat.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameFlow.Api.Hubs;

/// <summary>
/// Gerçek zamanlı sohbet. Mesaj gönderme/düzenleme/silme işleri
/// <see cref="IChatService"/> üzerinden yürür; hub yalnızca taşıma katmanıdır.
/// Böylece aynı iş kuralları REST uç noktalarıyla paylaşılır.
/// </summary>
[Authorize]
public class ChatHub(
    IChatService chatService,
    ICurrentUserService currentUser,
    ILogger<ChatHub> logger) : Hub<IChatClient>
{
    /// <summary>Odaya katılır. Yetki denetimi servis katmanında yapılır.</summary>
    public async Task JoinRoom(Guid roomId)
    {
        await chatService.EnsureCanAccessRoomAsync(roomId, Context.ConnectionAborted);

        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.ChatRoom(roomId));

        logger.LogDebug(
            "Kullanıcı {UserId} {RoomId} odasına katıldı.",
            currentUser.UserId,
            roomId);
    }

    public async Task LeaveRoom(Guid roomId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.ChatRoom(roomId));

    /// <summary>
    /// Mesaj gönderir. Yayın servis katmanındaki <see cref="IChatNotifier"/>
    /// üzerinden yapıldığı için burada ayrıca gruba gönderim yapılmaz.
    /// </summary>
    public async Task<MessageDto> SendMessage(Guid roomId, SendMessageRequest request)
        => await chatService.SendMessageAsync(roomId, request, Context.ConnectionAborted);

    public async Task<MessageDto> EditMessage(Guid roomId, Guid messageId, EditMessageRequest request)
        => await chatService.EditMessageAsync(roomId, messageId, request, Context.ConnectionAborted);

    public async Task DeleteMessage(Guid roomId, Guid messageId)
        => await chatService.DeleteMessageAsync(roomId, messageId, Context.ConnectionAborted);

    /// <summary>Okundu bilgisini işaretler ve odanın kalan okunmamış sayısını döner.</summary>
    public async Task<int> MarkAsRead(Guid roomId, MarkMessagesReadRequest request)
        => await chatService.MarkAsReadAsync(roomId, request, Context.ConnectionAborted);

    /// <summary>
    /// "Yazıyor..." göstergesi. Kalıcı bir veri olmadığı için veritabanına
    /// yazılmaz, yalnızca odadaki diğer istemcilere iletilir.
    /// </summary>
    public async Task NotifyTyping(Guid roomId, bool isTyping)
    {
        if (currentUser.UserId is not { } userId)
        {
            return;
        }

        await Clients.GroupExcept(HubGroups.ChatRoom(roomId), Context.ConnectionId)
            .UserTyping(roomId, userId, isTyping);
    }
}

/// <summary>
/// İstemcinin dinlediği sohbet olayları. Kuvvetli tiplenmiş hub kullanıldığı için
/// olay adları derleme zamanında denetlenir.
/// </summary>
public interface IChatClient
{
    Task MessageReceived(MessageDto message);

    Task MessageEdited(MessageDto message);

    Task MessageDeleted(Guid roomId, Guid messageId);

    Task MessagesRead(Guid roomId, Guid userId, IReadOnlyCollection<Guid> messageIds);

    Task UserTyping(Guid roomId, Guid userId, bool isTyping);
}
