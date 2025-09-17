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

        // Extract session token from header
        var sessionToken = context.Request.Headers["X-Session-Token"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(sessionToken))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing session token");
            return;
        }

        var userContext = _authService.ValidateSession(sessionToken);
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
            "/api/fake-login",
            "/api/health",
            "/api/version",
            "/swagger",
            "/swagger/",
            "/"
        };

        return skipPaths.Any(skipPath => path.StartsWith(skipPath));
    }
}