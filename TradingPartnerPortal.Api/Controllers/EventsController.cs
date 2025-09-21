using Microsoft.AspNetCore.Mvc;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Infrastructure.Extensions;
using System.Diagnostics;

namespace TradingPartnerPortal.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly ISseEventService _sseEventService;

    public EventsController(ISseEventService sseEventService)
    {
        _sseEventService = sseEventService;
    }

    /// <summary>
    /// Server-Sent Events stream endpoint for real-time updates
    /// </summary>
    [HttpGet("stream")]
    public async Task Stream([FromHeader(Name = "Last-Event-ID")] string? lastEventId = null)
    {
        try
        {
            var userContext = this.GetUserContext();
            await _sseEventService.StreamAsync(HttpContext, userContext, lastEventId);
        }
        catch (OperationCanceledException)
        {
            // Normal disconnection - no action needed
        }
        catch (Exception ex)
        {
            // Log error but don't return error response as SSE connection may already be started
            var traceId = Activity.Current?.TraceId.ToString();
            // TODO: Add proper logging here when logger is available
            Console.WriteLine($"SSE Stream error: {ex.Message}, TraceId: {traceId}");
        }
    }
}