using GameFlow.Application.Features.Sprints;
using GameFlow.Application.Features.Sprints.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>Sprint yönetimi ve raporları.</summary>
public class SprintsController(ISprintService sprintService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SprintSummaryDto>>> GetList(
        [FromQuery] SprintListRequest request,
        CancellationToken cancellationToken)
        => Ok(await sprintService.GetListAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SprintDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sprintService.GetByIdAsync(id, cancellationToken));

    /// <summary>Sprint ilerleme/kapanış raporu.</summary>
    [HttpGet("{id:guid}/report")]
    public async Task<ActionResult<SprintReportDto>> GetReport(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await sprintService.GetReportAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SprintDetailDto>> Create(
        CreateSprintRequest request,
        CancellationToken cancellationToken)
    {
        var sprint = await sprintService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sprint.Id }, sprint);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SprintDetailDto>> Update(
        Guid id,
        UpdateSprintRequest request,
        CancellationToken cancellationToken)
        => Ok(await sprintService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sprintService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Sprinti başlatır.</summary>
    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<SprintDetailDto>> Start(Guid id, CancellationToken cancellationToken)
        => Ok(await sprintService.StartAsync(id, cancellationToken));

    /// <summary>Sprinti tamamlar ve kapanış raporunu döner.</summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<SprintReportDto>> Complete(
        Guid id,
        CompleteSprintRequest request,
        CancellationToken cancellationToken)
        => Ok(await sprintService.CompleteAsync(id, request, cancellationToken));
}
