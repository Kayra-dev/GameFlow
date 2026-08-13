using GameFlow.Application.Features.Reports;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>Raporlama ekranlarının grafik verileri.</summary>
public class ReportsController(IReportService reportService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReportsDto>> Get(
        [FromQuery] ReportRequest request,
        CancellationToken cancellationToken)
        => Ok(await reportService.GetAsync(request, cancellationToken));
}
