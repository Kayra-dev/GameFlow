using GameFlow.Api.Extensions;
using GameFlow.Application.Features.Projects;
using GameFlow.Application.Features.Projects.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Proje yönetimi. Oluşturma ve silme yalnızca yöneticilere açıktır; diğer
/// işlemlerde proje üyeliği ve proje yöneticiliği servis katmanında denetlenir.
/// </summary>
public class ProjectsController(IProjectService projectService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> GetList(
        [FromQuery] ProjectListRequest request,
        CancellationToken cancellationToken)
        => Ok(await projectService.GetListAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await projectService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<ActionResult<ProjectDetailDto>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> Update(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
        => Ok(await projectService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await projectService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ProjectDetailDto>> AddMembers(
        Guid id,
        AddProjectMembersRequest request,
        CancellationToken cancellationToken)
        => Ok(await projectService.AddMembersAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await projectService.RemoveMemberAsync(id, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>Üyenin proje yöneticiliği yetkisini değiştirir.</summary>
    [HttpPut("{id:guid}/members/{userId:guid}/manager")]
    public async Task<IActionResult> SetManager(
        Guid id,
        Guid userId,
        [FromQuery] bool isManager,
        CancellationToken cancellationToken)
    {
        await projectService.SetMemberManagerAsync(id, userId, isManager, cancellationToken);
        return NoContent();
    }
}
