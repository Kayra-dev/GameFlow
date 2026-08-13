using GameFlow.Domain.Common;

namespace GameFlow.Domain.Entities;

/// <summary>Sohbet mesajı. Düzenlenebilir, silinebilir ve okundu bilgisi tutulur.</summary>
public class Message : BaseEntity, ISoftDeletable
{
    public Guid ChatRoomId { get; set; }

    public Guid SenderId { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsEdited { get; set; }

    public DateTime? EditedAt { get; set; }

    public Guid? ReplyToMessageId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public ChatRoom ChatRoom { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public Message? ReplyToMessage { get; set; }

    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    public ICollection<MessageRead> Reads { get; set; } = new List<MessageRead>();
}
