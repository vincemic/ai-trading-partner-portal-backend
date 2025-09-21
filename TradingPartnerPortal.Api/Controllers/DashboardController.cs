using Microsoft.AspNetCore.Mvc;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Infrastructure.Extensions;
using System.Diagnostics;

namespace TradingPartnerPortal.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IAdvancedMetricsService _advancedMetricsService;

    public DashboardController(IDashboardService dashboardService, IAdvancedMetricsService advancedMetricsService)
    {
        _dashboardService = dashboardService;
        _advancedMetricsService = advancedMetricsService;
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

    /// <summary>
    /// Get connection health metrics
    /// </summary>
    [HttpGet("connection/health")]
    public async Task<ActionResult<IReadOnlyList<ConnectionHealthPointDto>>> GetConnectionHealth(
        [FromQuery] DateTime? from = null, 
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var userContext = this.GetUserContext();
            var fromDate = from ?? DateTime.UtcNow.AddHours(-24);
            var toDate = to ?? DateTime.UtcNow;

            var connectionHealth = await _advancedMetricsService.GetConnectionHealthAsync(userContext.PartnerId, fromDate, toDate);
            return Ok(connectionHealth);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Get current connection status
    /// </summary>
    [HttpGet("connection/status")]
    public async Task<ActionResult<ConnectionCurrentStatusDto>> GetConnectionStatus()
    {
        try
        {
            var userContext = this.GetUserContext();
            var connectionStatus = await _advancedMetricsService.GetConnectionStatusAsync(userContext.PartnerId);
            return Ok(connectionStatus);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Get throughput metrics
    /// </summary>
    [HttpGet("throughput")]
    public async Task<ActionResult<IReadOnlyList<ThroughputPointDto>>> GetThroughput(
        [FromQuery] DateTime? from = null, 
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var userContext = this.GetUserContext();
            var fromDate = from ?? DateTime.UtcNow.AddHours(-24);
            var toDate = to ?? DateTime.UtcNow;

            var throughput = await _advancedMetricsService.GetThroughputAsync(userContext.PartnerId, fromDate, toDate);
            return Ok(throughput);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Get large files
    /// </summary>
    [HttpGet("large-files")]
    public async Task<ActionResult<IReadOnlyList<LargeFileDto>>> GetLargeFiles(
        [FromQuery] DateTime? from = null, 
        [FromQuery] DateTime? to = null, 
        [FromQuery] int limit = 10)
    {
        try
        {
            var userContext = this.GetUserContext();
            var fromDate = from ?? DateTime.UtcNow.AddHours(-24);
            var toDate = to ?? DateTime.UtcNow;

            var query = new LargeFileQuery
            {
                From = fromDate,
                To = toDate,
                Limit = Math.Min(limit, 50) // Max 50 as per spec
            };

            var largeFiles = await _advancedMetricsService.GetLargeFilesAsync(userContext.PartnerId, query);
            return Ok(largeFiles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Get connection performance metrics
    /// </summary>
    [HttpGet("connection/performance")]
    public async Task<ActionResult<IReadOnlyList<ConnectionPerformancePointDto>>> GetConnectionPerformance(
        [FromQuery] DateTime? from = null, 
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var userContext = this.GetUserContext();
            var fromDate = from ?? DateTime.UtcNow.AddHours(-24);
            var toDate = to ?? DateTime.UtcNow;

            var performance = await _advancedMetricsService.GetConnectionPerformanceAsync(userContext.PartnerId, fromDate, toDate);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Get daily operations summary
    /// </summary>
    [HttpGet("daily-summary")]
    public async Task<ActionResult<IReadOnlyList<DailyOpsPointDto>>> GetDailySummary([FromQuery] int days = 7)
    {
        try
        {
            var userContext = this.GetUserContext();
            var query = new DailySummaryQuery
            {
                Days = Math.Min(days, 14) // Max 14 days as per spec
            };

            var dailySummary = await _advancedMetricsService.GetDailySummaryAsync(userContext.PartnerId, query);
            return Ok(dailySummary);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Get failure burst detection
    /// </summary>
    [HttpGet("failure-bursts")]
    public async Task<ActionResult<IReadOnlyList<FailureBurstPointDto>>> GetFailureBursts([FromQuery] int lookbackMinutes = 1440) // 24 hours default
    {
        try
        {
            var userContext = this.GetUserContext();
            var query = new FailureBurstQuery
            {
                Lookback = TimeSpan.FromMinutes(lookbackMinutes)
            };

            var failureBursts = await _advancedMetricsService.GetFailureBurstsAsync(userContext.PartnerId, query);
            return Ok(failureBursts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Get zero file window status
    /// </summary>
    [HttpGet("zero-file-window")]
    public async Task<ActionResult<ZeroFileWindowStatusDto>> GetZeroFileWindow([FromQuery] int windowHours = 4)
    {
        try
        {
            var userContext = this.GetUserContext();
            var query = new ZeroFileWindowQuery
            {
                Window = TimeSpan.FromHours(Math.Max(1, Math.Min(windowHours, 12))) // 1-12 hours as per spec
            };

            var zeroFileWindow = await _advancedMetricsService.GetZeroFileWindowStatusAsync(userContext.PartnerId, query);
            return Ok(zeroFileWindow);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }
}