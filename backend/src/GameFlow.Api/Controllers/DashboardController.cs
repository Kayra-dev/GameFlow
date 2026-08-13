using GameFlow.Application.Features.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>Dashboard'ın tüm kartlarını tek istekte döner.</summary>
public class DashboardController(IDashboardService dashboardService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(
        [FromQuery] DashboardRequest request,
        CancellationToken cancellationToken)
        => Ok(await dashboardService.GetAsync(request, cancellationToken));
}
