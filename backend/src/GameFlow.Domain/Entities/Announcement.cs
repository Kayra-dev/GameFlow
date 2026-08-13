using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>Admin tarafından yayınlanan duyuru. Dashboard'da gösterilir.</summary>
public class Announcement : BaseEntity, ISoftDeletable
{
    public Guid AuthorId { get; set; }

    /// <summary>null ise tüm stüdyoya yayınlanır.</summary>
    public Guid? ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Info;

    public bool IsPinned { get; set; }

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Bu tarihten sonra dashboard'da gösterilmez.</summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public User Author { get; set; } = null!;
    public Project? Project { get; set; }
}
