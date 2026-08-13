using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>Oyun projesi. Görevler, sprintler, toplantılar ve sohbetler proje altında yönetilir.</summary>
public class Project : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Görev anahtarlarında kullanılan kısa kod (örn. "ODY" → ODY-42). Tekildir.</summary>
    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    public string? CoverImageUrl { get; set; }

    public string ColorHex { get; set; } = "#8B5CF6";

    /// <summary>Oyun türü (örn. "Roguelike", "Platformer").</summary>
    public string? Genre { get; set; }

    /// <summary>Hedef platformlar, virgülle ayrılmış (örn. "PC, PS5, Xbox").</summary>
    public string? Platforms { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? TargetReleaseDate { get; set; }

    /// <summary>
    /// Proje içinde verilen son görev numarası. Yeni görev eklenirken atomik olarak artırılır.
    /// </summary>
    public int WorkItemCounter { get; set; }

    public Guid? CreatedById { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public User? CreatedBy { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
    public ICollection<Label> Labels { get; set; } = new List<Label>();
    public ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
    public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
    public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
}
