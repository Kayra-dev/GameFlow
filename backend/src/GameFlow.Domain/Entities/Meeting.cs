using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>Toplantı kaydı. Takvimde ve dashboard'da gösterilir.</summary>
public class Meeting : BaseEntity
{
    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public Guid OrganizerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public string? Location { get; set; }

    /// <summary>Çevrimiçi toplantı bağlantısı.</summary>
    public string? MeetingUrl { get; set; }

    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;

    public Project? Project { get; set; }
    public Team? Team { get; set; }
    public User Organizer { get; set; } = null!;
    public ICollection<MeetingAttendee> Attendees { get; set; } = new List<MeetingAttendee>();
}
