using GameFlow.Application.Features.Calendar;
using GameFlow.Application.Features.Calendar.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Takvim. Aylık, haftalık ve günlük görünümler aynı uç noktayı farklı
/// tarih aralıklarıyla çağırır.
/// </summary>
public class CalendarController(ICalendarService calendarService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CalendarItemDto>>> GetItems(
        [FromQuery] CalendarRangeRequest request,
        CancellationToken cancellationToken)
        => Ok(await calendarService.GetItemsAsync(request, cancellationToken));

    [HttpPost("events")]
    public async Task<ActionResult<CalendarItemDto>> CreateEvent(
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken)
        => Ok(await calendarService.CreateEventAsync(request, cancellationToken));

    [HttpPut("events/{id:guid}")]
    public async Task<ActionResult<CalendarItemDto>> UpdateEvent(
        Guid id,
        UpdateCalendarEventRequest request,
        CancellationToken cancellationToken)
        => Ok(await calendarService.UpdateEventAsync(id, request, cancellationToken));

    [HttpDelete("events/{id:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken cancellationToken)
    {
        await calendarService.DeleteEventAsync(id, cancellationToken);
        return NoContent();
    }
}
