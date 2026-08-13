using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>
/// Takvim kaydı. Görev deadline'ları ve sprint tarihleri sorgu anında türetilir;
/// bu tablo kullanıcıların elle eklediği ek etkinlikleri tutar.
/// </summary>
public class CalendarEvent : BaseEntity
{
    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? CreatedById { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CalendarEventType Type { get; set; } = CalendarEventType.Custom;

    public DateTime StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool IsAllDay { get; set; }

    public string ColorHex { get; set; } = "#3B82F6";

    public Project? Project { get; set; }
    public Team? Team { get; set; }
    public User? CreatedBy { get; set; }
}
