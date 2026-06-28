using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public class TokenRefreshFeature(
    IdentityDbContext db,
    ITokenService tokenService,
    ISessionValidator sessionValidator,
    TimeProvider timeProvider)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh", async (
                Request request,
                TokenRefreshFeature feature,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                request.IpAddress = httpContext.GetRemoteIpAddress();
                request.UserAgent = httpContext.Request.GetUserAgent();

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces<Response>()
            .WithTags("Auth")
            .WithSummary("Refresh token");
    }

    private async Task<FluentResult<Response>> ExecuteAsync(Request request, CancellationToken ct)
    {
        var tokenResult = await tokenService.GetPrincipalFromExpiredTokenAsync(request.AccessToken);
        if (!tokenResult.IsSuccess)
        {
            return FluentResult<Response>.ValidationProblem(nameof(request.AccessToken), "Invalid access token.");
        }

        var principal = tokenResult.Result;
        var userId = principal.GetUserId();
        var accessTokenId = principal.GetAccessTokenId();

        var userSession = await db.UserSessions
            .Include(x => x.User)
            .ThenInclude(x => x!.UserRoles)!
            .ThenInclude(userRoles => userRoles.Role!)
            .SingleOrDefaultAsync(t => t.UserId == userId && t.RefreshToken == request.RefreshToken, ct);

        if (userSession == null)
        {
            return FluentResult<Response>.ValidationProblem(nameof(request.RefreshToken), "Refresh token not found.");
        }

        if (userSession.Status == UserSessionStatus.Terminated)
        {
            return FluentResult<Response>.ValidationProblem(nameof(request.RefreshToken),
                "User session has been terminated.");
        }

        if (userSession.ExpiredAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            return FluentResult<Response>.ValidationProblem(nameof(request.RefreshToken),
                "User session has been expired.");
        }

        var newAccessTokenId = AccessTokenId.Create();
        var newAuthTokens = await tokenService.GenerateTokensAsync(userId, newAccessTokenId, ct);

        userSession.AccessTokenId = newAccessTokenId;
        userSession.RefreshToken = newAuthTokens.RefreshToken;
        userSession.IpAddress = request.IpAddress;
        userSession.UserAgent = request.UserAgent;

        db.UserSessions.Update(userSession);
        await db.SaveChangesAsync(ct);

        // Swap cache keys: old token is no longer valid, new one is
        await sessionValidator.RemoveAsync(accessTokenId, ct);
        await sessionValidator.SetAsync(newAccessTokenId, userSession.ExpiredAt, ct);

        return FluentResult<Response>.Success(
            new Response
            {
                AccessToken = newAuthTokens.AccessToken,
                RefreshToken = newAuthTokens.RefreshToken
            }
        );
    }

    public class Request
    {
        [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; set; }
        [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; set; }
        [JsonIgnore] public IpAddress? IpAddress { get; set; }
        [JsonIgnore] public UserAgent UserAgent { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; set; }
        [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; set; }
    }
}
