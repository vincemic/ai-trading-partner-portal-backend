using Microsoft.AspNetCore.Mvc;
using TradingPartnerPortal.Application.DTOs;
using TradingPartnerPortal.Application.Services;
using TradingPartnerPortal.Infrastructure.Extensions;
using System.Diagnostics;

namespace TradingPartnerPortal.Api.Controllers;

[ApiController]
[Route("api/keys")]
public class KeysController : ControllerBase
{
    private readonly IKeyService _keyService;

    public KeysController(IKeyService keyService)
    {
        _keyService = keyService;
    }

    /// <summary>
    /// List all keys for the authenticated partner
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KeySummaryDto>>> ListKeys()
    {
        try
        {
            var userContext = this.GetUserContext();
            var keys = await _keyService.ListAsync(userContext.PartnerId);
            return Ok(keys);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Upload a new public key
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<KeySummaryDto>> UploadKey([FromBody] UploadKeyRequest request)
    {
        try
        {
            var userContext = this.GetUserContext();

            if (userContext.Role != "PartnerAdmin")
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only PartnerAdmin can upload keys" });
            }

            var (key, audit) = await _keyService.UploadAsync(userContext.PartnerId, request, userContext);
            return Ok(key);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = new { code = "CONFLICT", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Generate a new key pair
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateKeyResponse>> GenerateKey([FromBody] GenerateKeyRequest request)
    {
        try
        {
            var userContext = this.GetUserContext();

            if (userContext.Role != "PartnerAdmin")
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only PartnerAdmin can generate keys" });
            }

            var (response, audit) = await _keyService.GenerateAsync(userContext.PartnerId, request, userContext);
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

    /// <summary>
    /// Revoke a key
    /// </summary>
    [HttpPost("{keyId}/revoke")]
    public async Task<IActionResult> RevokeKey(Guid keyId, [FromBody] RevokeKeyRequest request)
    {
        try
        {
            var userContext = this.GetUserContext();

            if (userContext.Role != "PartnerAdmin")
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only PartnerAdmin can revoke keys" });
            }

            var audit = await _keyService.RevokeAsync(userContext.PartnerId, keyId, request, userContext);
            return Ok(new { success = true, auditId = audit.AuditId });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = new { code = "CONFLICT", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }

    /// <summary>
    /// Promote a key to primary
    /// </summary>
    [HttpPost("{keyId}/promote")]
    public async Task<IActionResult> PromoteKey(Guid keyId)
    {
        try
        {
            var userContext = this.GetUserContext();

            if (userContext.Role != "PartnerAdmin")
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only PartnerAdmin can promote keys" });
            }

            var audit = await _keyService.PromoteAsync(userContext.PartnerId, keyId, userContext);
            if (audit == null)
            {
                return Ok(new { success = true, message = "Key is already primary" });
            }

            return Ok(new { success = true, auditId = audit.AuditId });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = new { code = "CONFLICT", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message, traceId = Activity.Current?.TraceId.ToString() } });
        }
    }
}