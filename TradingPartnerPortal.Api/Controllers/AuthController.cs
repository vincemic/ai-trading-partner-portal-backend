using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TradingPartnerPortal.Infrastructure.Authentication;
using TradingPartnerPortal.Infrastructure.Extensions;

namespace TradingPartnerPortal.Api.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly FakeAuthenticationService _authService;

    public AuthController(FakeAuthenticationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Create a fake session token for testing purposes
    /// </summary>
    [HttpPost("fake-login")]
    public ActionResult<FakeAuthenticationService.FakeLoginResponse> FakeLogin(
        [FromBody] FakeAuthenticationService.FakeLoginRequest request)
    {
        try
        {
            var response = _authService.CreateSession(request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow.ToString("O") });
    }

    /// <summary>
    /// Version information
    /// </summary>
    [HttpGet("version")]
    public IActionResult Version()
    {
        return Ok(new { 
            version = "1.0.0", 
            build = "pilot", 
            timestamp = DateTime.UtcNow.ToString("O") 
        });
    }
}