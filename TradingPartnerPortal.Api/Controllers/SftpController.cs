using Microsoft.AspNetCore.Mvc;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Infrastructure.Extensions;
using System.Diagnostics;

namespace TradingPartnerPortal.Api.Controllers;

[ApiController]
[Route("api/sftp")]
public class SftpController : ControllerBase
{
    private readonly ISftpCredentialService _sftpService;

    public SftpController(ISftpCredentialService sftpService)
    {
        _sftpService = sftpService;
    }

    /// <summary>
    /// Get SFTP credential metadata (no password)
    /// </summary>
    [HttpGet("credential")]
    public async Task<ActionResult<SftpCredentialMetadataDto>> GetCredentialMetadata()
    {
        try
        {
            var userContext = this.GetUserContext();
            var metadata = await _sftpService.GetMetadataAsync(userContext.PartnerId);

            if (metadata == null)
            {
                return NotFound(new { error = new { code = "NOT_FOUND", message = "No SFTP credential found", traceId = Activity.Current?.TraceId.ToString() } });
            }

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Rotate SFTP password
    /// </summary>
    [HttpPost("credential/rotate")]
    public async Task<ActionResult<RotatePasswordResponse>> RotatePassword([FromBody] RotatePasswordRequest request)
    {
        try
        {
            var userContext = this.GetUserContext();

            if (userContext.Role != "PartnerAdmin")
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only PartnerAdmin can rotate passwords" });
            }

            var (response, audit) = await _sftpService.RotateAsync(userContext.PartnerId, request, userContext);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }
}