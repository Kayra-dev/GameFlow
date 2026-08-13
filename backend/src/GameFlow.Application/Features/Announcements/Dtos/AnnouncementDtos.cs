using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Announcements.Dtos;

public record AnnouncementDto(
    Guid Id,
    string Title,
    string Content,
    AnnouncementPriority Priority,
    bool IsPinned,
    DateTime PublishedAt,
    DateTime? ExpiresAt,
    UserSummaryDto Author,
    Guid? ProjectId,
    string? ProjectName);

public class CreateAnnouncementRequest
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Info;

    public bool IsPinned { get; set; }

    /// <summary>null ise tüm stüdyoya yayınlanır.</summary>
    public Guid? ProjectId { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class UpdateAnnouncementRequest : CreateAnnouncementRequest;

public class AnnouncementListRequest
{
    public Guid? ProjectId { get; set; }

    /// <summary>Süresi geçmiş duyuruları da getir.</summary>
    public bool IncludeExpired { get; set; }
}
