using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Peritus.Api.Extensions;
using Peritus.Guard.Extensions;
using Peritus.Identity.Features.Accounts;
using Peritus.Identity.Persistence;
using Peritus.Identity.RestContracts.Accounts;
using Peritus.Identity.Types;

namespace Peritus.Api.Endpoints.Accounts;

public static class SessionEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/accounts/sessions")
            .WithTags("Sessions")
            .RequireAuthorization();

        group.MapGet("/", HandleListUserSessionsAsync)
            .Produces<List<SessionResponse>>()
            .WithSummary("List user sessions");

        group.MapPost("/{id}/revoke", HandleRevokeUserSessionAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Revoke user session");

        group.MapPost("/revoke-all", HandleRevokeAllSessionsAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Revoke all sessions");
    }

    private static async Task<IResult> HandleListUserSessionsAsync(
        SessionListFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new SessionListFeature.Context
            {
                UserId = principal.GetUserId()
            }, ct);

        return result.ToResult(x =>
            x.Select(s => new SessionResponse
            {
                Id = s.Id.Value.ToString(),
                UserAgent = s.UserAgent.Value,
                IpAddress = s.IpAddress?.Value,
                ExpiredAt = s.ExpiredAt
            }).ToList()
        );
    }

    private static async Task<IResult> HandleRevokeUserSessionAsync(
        string id,
        SessionRevokeFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new SessionRevokeFeature.Context
            {
                UserId = principal.GetUserId(),
                SessionId = new UserSessionId(Guid.Parse(id))
            }, ct);

        return result.ToResult();
    }

    private static async Task<IResult> HandleRevokeAllSessionsAsync(
        SessionRevokeAllFeature feature,
        ClaimsPrincipal principal,
        IdentityDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var accessTokenId = principal.GetAccessTokenId();

        var currentSession = await db.UserSessions
            .SingleAsync(s => s.AccessTokenId == accessTokenId && s.UserId == userId, ct);

        var result = await feature.ExecuteAsync(
            new SessionRevokeAllFeature.Context
            {
                UserId = userId,
                CurrentSessionId = currentSession.Id
            }, ct);

        return result.ToResult();
    }
}
