using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Calendar.Dtos;

/// <summary>Takvim sorgusu. Ay/hafta/gün görünümleri aynı uç noktayı kullanır.</summary>
public class CalendarRangeRequest
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    /// <summary>Yalnızca oturum sahibini ilgilendiren öğeler.</summary>
    public bool OnlyMine { get; set; }

    /// <summary>Gösterilecek öğe türleri. Boşsa tümü gelir.</summary>
    public List<CalendarEventType> Types { get; set; } = [];
}

public class CreateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CalendarEventType Type { get; set; } = CalendarEventType.Custom;

    public DateTime StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool IsAllDay { get; set; }

    public string ColorHex { get; set; } = "#3B82F6";

    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }
}

public class UpdateCalendarEventRequest : CreateCalendarEventRequest;

public class CreateMeetingRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public string? Location { get; set; }

    public string? MeetingUrl { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public List<Guid> AttendeeIds { get; set; } = [];
}

public class UpdateMeetingRequest : CreateMeetingRequest
{
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
}

public class RespondToMeetingRequest
{
    public bool IsAccepted { get; set; }
}

public class MeetingListRequest
{
    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public MeetingStatus? Status { get; set; }

    /// <summary>Yalnızca gelecekteki toplantılar.</summary>
    public bool OnlyUpcoming { get; set; }

    /// <summary>Yalnızca oturum sahibinin katılımcı veya düzenleyici olduğu toplantılar.</summary>
    public bool OnlyMine { get; set; }
}
