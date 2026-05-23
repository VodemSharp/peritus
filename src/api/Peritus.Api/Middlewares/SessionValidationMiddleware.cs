using System.Net;
using Peritus.Guard.Extensions;
using Peritus.Identity.Services.Abstractions;

namespace Peritus.Api.Middlewares;

public class SessionValidationMiddleware(ISessionValidator sessionValidator) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

        if (!isAuthenticated)
        {
            await next(context);
            return;
        }

        var accessTokenId = context.User.GetAccessTokenId();
        var isValid = await sessionValidator.IsValidAsync(accessTokenId);

        if (isValid)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.Headers.Append("WWW-Authenticate", "Bearer error=\"invalid_token\"");
    }
}
