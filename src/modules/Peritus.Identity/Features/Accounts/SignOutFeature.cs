using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Accounts;

public partial class SignOutFeature(
    ILogger<SignOutFeature> logger,
    ISessionValidator sessionValidator,
    IUserSessionService userSessionService,
    IdentityDbContext db)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/accounts/signout", async (
                SignOutFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                var request = new Request
                {
                    AccessTokenId = principal.GetAccessTokenId()
                };

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization()
            .WithTags("Sessions")
            .WithSummary("Sign out");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var userSession = await userSessionService.FindByAccessTokenIdAsync(request.AccessTokenId, ct);

        if (userSession != null)
        {
            userSession.Status = UserSessionStatus.Terminated;
            db.UserSessions.Update(userSession);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            LogUserSessionWasNotFound(request.AccessTokenId.Value);
        }

        await sessionValidator.RemoveAsync(request.AccessTokenId, ct);

        return FluentResult.Success();
    }

    [LoggerMessage(LogLevel.Warning, "User session was not found: {AccessTokenId}")]
    private partial void LogUserSessionWasNotFound(Guid accessTokenId);

    public class Request
    {
        [JsonIgnore] public AccessTokenId AccessTokenId { get; set; }
    }
}
