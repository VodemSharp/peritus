using System.Security.Claims;
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

namespace Peritus.Identity.Features.Accounts;

public class SessionRevokeAllFeature(IdentityDbContext db, ISessionValidator sessionValidator)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/accounts/sessions/revoke-all", async (
                SessionRevokeAllFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                var request = new Request
                {
                    UserId = principal.GetUserId(),
                    AccessTokenId = principal.GetAccessTokenId()
                };

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization()
            .WithTags("Sessions")
            .WithSummary("Revoke all sessions");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var currentSession = await db.UserSessions
            .AsNoTracking()
            .Where(x => x.UserId == request.UserId && x.AccessTokenId == request.AccessTokenId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        var sessions = await db.UserSessions
            .Where(x => x.UserId == request.UserId
                        && x.Status == UserSessionStatus.Confirmed
                        && x.Id != currentSession)
            .ToListAsync(ct);

        foreach (var session in sessions)
        {
            session.Status = UserSessionStatus.Terminated;
            db.UserSessions.Update(session);

            // Invalidate each session cache entry
            await sessionValidator.RemoveAsync(session.AccessTokenId, ct);
        }

        await db.SaveChangesAsync(ct);

        return FluentResult.Success();
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
        [JsonIgnore] public AccessTokenId AccessTokenId { get; set; }
    }
}
