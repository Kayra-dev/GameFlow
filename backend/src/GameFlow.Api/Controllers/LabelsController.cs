using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.WorkItems;
using GameFlow.Application.Features.WorkItems.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>Proje bazlı görev etiketleri.</summary>
[Route("api/projects/{projectId:guid}/labels")]
public class LabelsController(ILabelService labelService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LabelDto>>> GetList(
        Guid projectId,
        CancellationToken cancellationToken)
        => Ok(await labelService.GetListAsync(projectId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<LabelDto>> Create(
        Guid projectId,
        CreateLabelRequest request,
        CancellationToken cancellationToken)
        => Ok(await labelService.CreateAsync(projectId, request, cancellationToken));

    [HttpDelete("{labelId:guid}")]
    public async Task<IActionResult> Delete(
        Guid projectId,
        Guid labelId,
        CancellationToken cancellationToken)
    {
        await labelService.DeleteAsync(projectId, labelId, cancellationToken);
        return NoContent();
    }
}
