using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace TradingPartnerPortal.Api.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{



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