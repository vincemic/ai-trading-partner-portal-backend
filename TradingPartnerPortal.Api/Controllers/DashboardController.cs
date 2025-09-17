using Microsoft.AspNetCore.Mvc;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Infrastructure.Extensions;
using System.Diagnostics;

namespace TradingPartnerPortal.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get dashboard summary metrics
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        try
        {
            var userContext = this.GetUserContext();
            var summary = await _dashboardService.GetSummaryAsync(userContext.PartnerId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Get time series data for file counts
    /// </summary>
    [HttpGet("timeseries")]
    public async Task<ActionResult<TimeSeriesResponse>> GetTimeSeries([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        try
        {
            var userContext = this.GetUserContext();
            var fromDate = from ?? DateTime.UtcNow.AddHours(-48);
            var toDate = to ?? DateTime.UtcNow;

            var timeSeries = await _dashboardService.GetTimeSeriesAsync(userContext.PartnerId, fromDate, toDate);
            return Ok(timeSeries);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Get top error categories
    /// </summary>
    [HttpGet("errors/top")]
    public async Task<ActionResult<TopErrorsResponse>> GetTopErrors(
        [FromQuery] DateTime? from = null, 
        [FromQuery] DateTime? to = null, 
        [FromQuery] int top = 5)
    {
        try
        {
            var userContext = this.GetUserContext();
            var fromDate = from ?? DateTime.UtcNow.AddHours(-24);
            var toDate = to ?? DateTime.UtcNow;

            var topErrors = await _dashboardService.GetTopErrorsAsync(userContext.PartnerId, fromDate, toDate, top);
            return Ok(topErrors);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }
}