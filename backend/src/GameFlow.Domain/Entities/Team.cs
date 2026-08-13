using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>
/// Stüdyo içindeki departman/takım (Yazılım, Tasarım, Ses, Test ...).
/// Takımlar stüdyo genelindedir ve birden fazla projede görev alabilir.
/// </summary>
public class Team : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TeamCategory Category { get; set; }

    /// <summary>Arayüzdeki takım rengi (#RRGGBB).</summary>
    public string ColorHex { get; set; } = "#6366F1";

    /// <summary>lucide-react ikon anahtarı.</summary>
    public string? IconKey { get; set; }

    /// <summary>
    /// Takım lideri. <see cref="TeamMember.Role"/> ile tutarlılığı servis katmanı korur;
    /// sık yapılan sorgularda ekstra join gerektirmemesi için burada da tutulur.
    /// </summary>
    public Guid? LeaderId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public User? Leader { get; set; }

    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
    public ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
    public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
}
