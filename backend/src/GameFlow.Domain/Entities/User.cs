using GameFlow.Domain.Common;

namespace GameFlow.Domain.Entities;

/// <summary>
/// Sistem kullanıcısı. Kayıt ekranı bulunmadığından yalnızca Admin tarafından oluşturulur.
/// </summary>
public class User : BaseEntity, ISoftDeletable
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Giriş için kullanılan e-posta. Küçük harfe normalize edilerek tekil tutulur.</summary>
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Unvan (örn. "Gameplay Programmer", "Concept Artist").</summary>
    public string? JobTitle { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>SignalR bağlantı durumundan türetilen çevrimiçi göstergesi.</summary>
    public bool IsOnline { get; set; }

    public DateTime? LastSeenAt { get; set; }

    /// <summary>Kullanıcının ilk girişte şifresini değiştirmesi gerekiyor mu.</summary>
    public bool MustChangePassword { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public Role Role { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
    public ICollection<WorkItem> AssignedWorkItems { get; set; } = new List<WorkItem>();
    public ICollection<WorkItem> ReportedWorkItems { get; set; } = new List<WorkItem>();
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskAttachment> UploadedAttachments { get; set; } = new List<TaskAttachment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();
    public ICollection<Meeting> OrganizedMeetings { get; set; } = new List<Meeting>();
    public ICollection<MeetingAttendee> MeetingAttendances { get; set; } = new List<MeetingAttendee>();
    public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
    public ICollection<ActivityLog> Activities { get; set; } = new List<ActivityLog>();
}
