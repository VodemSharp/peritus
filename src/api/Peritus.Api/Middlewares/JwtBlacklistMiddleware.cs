using System.Net;
using Peritus.Guard.Extensions;
using Peritus.Guard.Services.Abstractions;

namespace Peritus.Api.Middlewares;

public class JwtBlacklistMiddleware(ITokenRevoker tokenRevoker) : IMiddleware
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
        var isTokenRevoked = await tokenRevoker.IsRevokedAsync(accessTokenId);

        if (!isTokenRevoked)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
    }
}
