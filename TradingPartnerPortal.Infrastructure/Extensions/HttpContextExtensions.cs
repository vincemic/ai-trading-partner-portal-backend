using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TradingPartnerPortal.Application.Models;

namespace TradingPartnerPortal.Infrastructure.Extensions;

public static class HttpContextExtensions
{
    public static UserContext GetUserContext(this HttpContext context)
    {
        if (context.Items.TryGetValue("UserContext", out var userContext) && userContext is UserContext user)
        {
            return user;
        }

        throw new UnauthorizedAccessException("User context not found");
    }
}

public static class ControllerExtensions
{
    public static UserContext GetUserContext(this ControllerBase controller)
    {
        return controller.HttpContext.GetUserContext();
    }
}