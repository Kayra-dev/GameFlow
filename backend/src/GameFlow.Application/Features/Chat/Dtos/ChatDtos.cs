using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Chat.Dtos;

/// <summary>Sohbet odası listesi öğesi.</summary>
public record ChatRoomDto(
    Guid Id,
    string Name,
    ChatRoomType Type,
    string? Description,
    Guid? TeamId,
    Guid? ProjectId,
    string? ColorHex,
    int UnreadCount,
    MessageDto? LastMessage);

public record MessageDto(
    Guid Id,
    Guid ChatRoomId,
    string Content,
    UserSummaryDto Sender,
    DateTime CreatedAt,
    bool IsEdited,
    DateTime? EditedAt,
    Guid? ReplyToMessageId,
    string? ReplyToPreview,
    string? ReplyToSenderName,
    IReadOnlyList<AttachmentDto> Attachments,
    int ReadByCount,
    bool IsReadByMe);

/// <summary>
/// Sohbet geçmişi sayfası. Sohbette klasik sayfa numarası yerine imleç (cursor)
/// kullanılır; yeni mesaj geldiğinde sayfa kaymaz.
/// </summary>
public record MessagePageDto(
    IReadOnlyList<MessageDto> Items,
    bool HasMore,
    /// <summary>Bir sonraki sayfa için kullanılacak imleç (en eski mesajın zamanı).</summary>
    DateTime? NextCursor);

/// <summary>Bir mesajı kimlerin okuduğu.</summary>
public record MessageReadReceiptDto(UserSummaryDto User, DateTime ReadAt);

public record OnlineUserDto(Guid UserId, string FullName, string? AvatarUrl, DateTime? LastSeenAt);
