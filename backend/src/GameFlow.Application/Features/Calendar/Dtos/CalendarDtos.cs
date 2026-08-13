using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Calendar.Dtos;

/// <summary>
/// Takvimde gösterilen tek bir öğe. Görev son tarihleri, sprint tarihleri ve
/// toplantılar sorgu anında türetilir; elle eklenen etkinlikler CalendarEvents
/// tablosundan gelir. Hepsi bu tek gösterimde birleşir.
/// </summary>
public record CalendarItemDto(
    Guid Id,
    string Title,
    CalendarEventType Type,
    DateTime StartsAt,
    DateTime? EndsAt,
    bool IsAllDay,
    string ColorHex,
    /// <summary>İstemci içi yol (örn. "/gorevler/ODY-42").</summary>
    string? Link,
    Guid? ProjectId,
    string? ProjectName,
    Guid? TeamId,
    string? TeamName);

public record MeetingDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    string? Location,
    string? MeetingUrl,
    MeetingStatus Status,
    UserSummaryDto Organizer,
    Guid? ProjectId,
    string? ProjectName,
    Guid? TeamId,
    string? TeamName,
    IReadOnlyList<MeetingAttendeeDto> Attendees,
    /// <summary>Oturum sahibinin katılım yanıtı: null = yanıt yok.</summary>
    bool? MyResponse);

public record MeetingAttendeeDto(UserSummaryDto User, bool? IsAccepted, DateTime? RespondedAt);
