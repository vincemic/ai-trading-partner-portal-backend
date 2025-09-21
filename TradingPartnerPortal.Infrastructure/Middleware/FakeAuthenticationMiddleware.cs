using Microsoft.AspNetCore.Http;
using TradingPartnerPortal.Application.Models;
using TradingPartnerPortal.Infrastructure.Authentication;

namespace TradingPartnerPortal.Infrastructure.Middleware;

public class FakeAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly FakeAuthenticationService _authService;

    public FakeAuthenticationMiddleware(RequestDelegate next, FakeAuthenticationService authService)
    {
        _next = next;
        _authService = authService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for certain paths
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (ShouldSkipAuthentication(path))
        {
            await _next(context);
            return;
        }

        // Extract session token from header or query parameter (for SSE compatibility)
        var sessionToken = context.Request.Headers["X-Session-Token"].FirstOrDefault();

        // Fallback to query parameter for SSE connections (EventSource doesn't support custom headers)
        if (string.IsNullOrEmpty(sessionToken))
        {
            sessionToken = context.Request.Query["token"].FirstOrDefault();
        }

        if (string.IsNullOrEmpty(sessionToken))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing session token");
            return;
        }

        UserContext? userContext = null;

        // Handle test tokens with predefined patterns
        if (IsTestToken(sessionToken))
        {
            userContext = CreateTestUserContext(sessionToken);
        }
        else
        {
            // Handle regular session tokens
            userContext = _authService.ValidateSession(sessionToken);
        }

        if (userContext == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid or expired session");
            return;
        }

        // Add user context to request
        context.Items["UserContext"] = userContext;

        await _next(context);
    }

    private static bool ShouldSkipAuthentication(string path)
    {
        var skipPaths = new[]
        {
            "/api/health",
            "/api/version",
            "/swagger",
            "/swagger/"
        };

        // Check for exact root path match separately
        if (path == "/")
        {
            return true;
        }

        return skipPaths.Any(skipPath => path.StartsWith(skipPath));
    }

    /// <summary>
    /// Determines if a token is a test token based on predefined patterns
    /// </summary>
    private static bool IsTestToken(string token)
    {
        return token.StartsWith("test-") || token.StartsWith("admin-") || token.StartsWith("user-");
    }

    /// <summary>
    /// Creates a UserContext for test tokens based on the token pattern
    /// </summary>
    private static UserContext CreateTestUserContext(string token)
    {
        // Default test partner ID
        var defaultPartnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        return token switch
        {
            "test-session-token" => new UserContext
            {
                UserId = "test-user",
                PartnerId = defaultPartnerId,
                Role = "PartnerUser"
            },
            "admin-session-token" => new UserContext
            {
                UserId = "admin-user",
                PartnerId = defaultPartnerId,
                Role = "PartnerAdmin"
            },
            "user-session-token" => new UserContext
            {
                UserId = "regular-user",
                PartnerId = defaultPartnerId,
                Role = "PartnerUser"
            },
            _ when token.StartsWith("test-admin-") => new UserContext
            {
                UserId = token.Replace("test-admin-", "admin-"),
                PartnerId = defaultPartnerId,
                Role = "PartnerAdmin"
            },
            _ when token.StartsWith("test-user-") => new UserContext
            {
                UserId = token.Replace("test-user-", "user-"),
                PartnerId = defaultPartnerId,
                Role = "PartnerUser"
            },
            _ => new UserContext
            {
                UserId = "test-user",
                PartnerId = defaultPartnerId,
                Role = "PartnerUser"
            }
        };
    }
}