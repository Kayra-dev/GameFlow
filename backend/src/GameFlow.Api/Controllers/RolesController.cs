using GameFlow.Application.Common.Interfaces;
using GameFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Api.Controllers;

/// <summary>Rol listesi. Roller sabittir; yalnızca okunur.</summary>
public class RolesController(IApplicationDbContext context) : ApiControllerBase
{
    public record RoleDto(SystemRole Id, string Name, string DisplayName, string? Description);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetList(CancellationToken cancellationToken)
        => Ok(await context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto((SystemRole)r.Id, r.Name, r.DisplayName, r.Description))
            .ToListAsync(cancellationToken));
}
