using GameFlow.Application.Features.Chat.Dtos;
using GameFlow.Application.Features.Shared.Dtos;

namespace GameFlow.Application.Features.Chat;

public interface IChatService
{
    /// <summary>Kullanıcının erişebildiği sohbet odaları (takım, lider, proje).</summary>
    Task<IReadOnlyList<ChatRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default);

    Task<ChatRoomDto> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default);

    /// <summary>Lider sohbet odası. Yalnızca takım liderleri ve yöneticiler erişebilir.</summary>
    Task<ChatRoomDto> GetLeadersRoomAsync(CancellationToken cancellationToken = default);

    Task<MessagePageDto> GetMessagesAsync(
        Guid roomId,
        MessageHistoryRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageDto> SendMessageAsync(
        Guid roomId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageDto> EditMessageAsync(
        Guid roomId,
        Guid messageId,
        EditMessageRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteMessageAsync(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>Mesajları okundu işaretler ve odanın yeni okunmamış sayısını döner.</summary>
    Task<int> MarkAsReadAsync(
        Guid roomId,
        MarkMessagesReadRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageReadReceiptDto>> GetReadReceiptsAsync(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>Sohbete dosya veya resim yükler ve mesaj olarak paylaşır.</summary>
    Task<MessageDto> SendAttachmentAsync(
        Guid roomId,
        Stream content,
        string fileName,
        string contentType,
        string? caption,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Odaya erişim yetkisini denetler. SignalR hub'ı, gruba katılım öncesinde
    /// bu kontrolü kullanır.
    /// </summary>
    Task EnsureCanAccessRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
}
