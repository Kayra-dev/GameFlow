using GameFlow.Application.Features.Calendar;
using GameFlow.Application.Features.Calendar.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>Toplantı yönetimi.</summary>
public class MeetingsController(IMeetingService meetingService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MeetingDto>>> GetList(
        [FromQuery] MeetingListRequest request,
        CancellationToken cancellationToken)
        => Ok(await meetingService.GetListAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MeetingDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await meetingService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<MeetingDto>> Create(
        CreateMeetingRequest request,
        CancellationToken cancellationToken)
    {
        var meeting = await meetingService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = meeting.Id }, meeting);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MeetingDto>> Update(
        Guid id,
        UpdateMeetingRequest request,
        CancellationToken cancellationToken)
        => Ok(await meetingService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await meetingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Katılım yanıtı verir.</summary>
    [HttpPost("{id:guid}/respond")]
    public async Task<ActionResult<MeetingDto>> Respond(
        Guid id,
        RespondToMeetingRequest request,
        CancellationToken cancellationToken)
        => Ok(await meetingService.RespondAsync(id, request, cancellationToken));
}
