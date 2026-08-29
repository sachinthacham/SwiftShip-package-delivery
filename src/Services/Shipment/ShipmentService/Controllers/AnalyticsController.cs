using BuildingBlocks.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentService.Application.Abstractions;

namespace ShipmentService.API.Controllers;

[ApiController]
[Route("api/shipments/analytics")]
[Authorize(Roles = Roles.DispatcherOrAdmin)]
public class AnalyticsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public AnalyticsController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    /// <summary>Fleet-wide shipment counts: totals by status, delivered/failed/SLA-breached today.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _shipmentService.GetAnalyticsSummaryAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Per-driver delivery counts and on-time rate.</summary>
    [HttpGet("drivers/performance")]
    public async Task<IActionResult> GetDriverPerformance(CancellationToken cancellationToken)
    {
        var result = await _shipmentService.GetDriverPerformanceAsync(cancellationToken);
        return Ok(result);
    }
}
