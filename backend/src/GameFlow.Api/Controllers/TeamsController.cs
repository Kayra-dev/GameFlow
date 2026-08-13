using GameFlow.Api.Extensions;
using GameFlow.Application.Features.Teams;
using GameFlow.Application.Features.Teams.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Takım yönetimi. Takım oluşturma/silme ve lider atama yalnızca yöneticilere açıktır;
/// güncelleme ve üye yönetiminde ayrıca takım liderliği kontrolü servis katmanında yapılır.
/// </summary>
public class TeamsController(ITeamService teamService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamSummaryDto>>> GetList(
        [FromQuery] TeamListRequest request,
        CancellationToken cancellationToken)
        => Ok(await teamService.GetListAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeamDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await teamService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<ActionResult<TeamDetailDto>> Create(
        CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var team = await teamService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
    }

    /// <summary>Takım bilgilerini günceller (yönetici veya takım lideri).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationExtensions.LeaderPolicy)]
    public async Task<ActionResult<TeamDetailDto>> Update(
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken)
        => Ok(await teamService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await teamService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Takım lideri atar veya kaldırır.</summary>
    [HttpPut("{id:guid}/leader")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<ActionResult<TeamDetailDto>> AssignLeader(
        Guid id,
        AssignLeaderRequest request,
        CancellationToken cancellationToken)
        => Ok(await teamService.AssignLeaderAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/members")]
    [Authorize(AuthenticationExtensions.LeaderPolicy)]
    public async Task<ActionResult<TeamDetailDto>> AddMembers(
        Guid id,
        AddTeamMembersRequest request,
        CancellationToken cancellationToken)
        => Ok(await teamService.AddMembersAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [Authorize(AuthenticationExtensions.LeaderPolicy)]
    public async Task<IActionResult> RemoveMember(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await teamService.RemoveMemberAsync(id, userId, cancellationToken);
        return NoContent();
    }
}
