namespace GameFlow.Domain.Entities;

/// <summary>Toplantı katılımcısı ve katılım durumu.</summary>
public class MeetingAttendee
{
    public Guid MeetingId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>null: yanıt yok, true: katılacak, false: katılmayacak.</summary>
    public bool? IsAccepted { get; set; }

    public DateTime? RespondedAt { get; set; }

    public Meeting Meeting { get; set; } = null!;
    public User User { get; set; } = null!;
}
