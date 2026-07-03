using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public partial class SessionRevokeFeature(
    IdentityDbContext db,
    ISessionValidator sessionValidator,
    ILogger<SessionRevokeFeature> logger)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/accounts/sessions/{id}/revoke", async (
                string id,
                SessionRevokeFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                var request = new Request
                {
                    UserId = principal.GetUserId(),
                    SessionId = new UserSessionId(Guid.Parse(id))
                };

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization()
            .WithTags("Sessions")
            .WithSummary("Revoke session");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var session = await db.UserSessions
            .Where(x => x.Id == request.SessionId && x.UserId == request.UserId)
            .SingleOrDefaultAsync(ct);

        if (session is null)
        {
            LogSessionNotFound(request.UserId.Value, request.SessionId.Value);
            return FluentResult.NotFound(IdentityErrorCodes.SessionNotFound);
        }

        session.Status = UserSessionStatus.Terminated;
        db.UserSessions.Update(session);
        await db.SaveChangesAsync(ct);

        // Invalidate cache so the token is rejected immediately
        await sessionValidator.RemoveAsync(session.AccessTokenId, ct);

        return FluentResult.Success();
    }

    [LoggerMessage(LogLevel.Warning, "Session not found for user {UserId}: {SessionId}")]
    private partial void LogSessionNotFound(Guid userId, Guid sessionId);

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
        [JsonIgnore] public UserSessionId SessionId { get; set; }
    }
}
