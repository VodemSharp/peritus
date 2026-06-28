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
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class TwoFactorDisableFeature(IdentityDbContext db, IUserService userService)
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/accounts/2fa/disable", async (
                TwoFactorDisableFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                var request = new Request
                {
                    UserId = principal.GetUserId()
                };

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .WithTags("TwoFactor");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(request.UserId, ct);
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;

        await db.ExecuteInTransactionAsync(async () =>
        {
            db.Users.Update(user);
            await db.SaveChangesAsync(ct);

            await db.UserRecoveryCodes
                .Where(r => r.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
        }, ct);

        return FluentResult.Success();
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
    }
}
