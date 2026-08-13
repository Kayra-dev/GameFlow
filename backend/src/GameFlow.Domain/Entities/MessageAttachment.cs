using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>Sohbette paylaşılan dosya veya resim.</summary>
public class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public AttachmentCategory Category { get; set; }

    public string Url { get; set; } = string.Empty;

    public Message Message { get; set; } = null!;
}
