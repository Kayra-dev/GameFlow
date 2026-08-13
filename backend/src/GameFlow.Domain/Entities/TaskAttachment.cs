using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>Göreve yüklenen dosya. Fiziksel dosya sunucuda saklanır, metadata burada tutulur.</summary>
public class TaskAttachment : BaseEntity
{
    public Guid WorkItemId { get; set; }

    public Guid? UploadedById { get; set; }

    /// <summary>Kullanıcının yüklediği özgün dosya adı.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Diskte tutulan çakışmasız ad (GUID + uzantı).</summary>
    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public AttachmentCategory Category { get; set; }

    /// <summary>İstemcinin dosyaya erişeceği göreli yol.</summary>
    public string Url { get; set; } = string.Empty;

    public WorkItem WorkItem { get; set; } = null!;
    public User? UploadedBy { get; set; }
}
