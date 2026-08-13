using GameFlow.Api.Extensions;
using GameFlow.Application.Features.Announcements;
using GameFlow.Application.Features.Announcements.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>Duyurular. Yayınlama, düzenleme ve silme yalnızca yöneticilere açıktır.</summary>
public class AnnouncementsController(IAnnouncementService announcementService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetList(
        [FromQuery] AnnouncementListRequest request,
        CancellationToken cancellationToken)
        => Ok(await announcementService.GetListAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnnouncementDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await announcementService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<ActionResult<AnnouncementDto>> Create(
        CreateAnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        var announcement = await announcementService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = announcement.Id }, announcement);
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<ActionResult<AnnouncementDto>> Update(
        Guid id,
        UpdateAnnouncementRequest request,
        CancellationToken cancellationToken)
        => Ok(await announcementService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await announcementService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
