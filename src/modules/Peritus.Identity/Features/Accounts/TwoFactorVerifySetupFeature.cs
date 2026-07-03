using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Helpers;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class TwoFactorVerifySetupFeature(
    IdentityDbContext db,
    IUserService userService)
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/accounts/2fa/confirm", async (
                Request request,
                TwoFactorVerifySetupFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                request.UserId = principal.GetUserId();

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .RequireAuthorization()
            .Produces<Response>()
            .WithTags("TwoFactor");
    }

    private async Task<FluentResult<Response>> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(request.UserId, ct);

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return FluentResult<Response>.ValidationMessage(IdentityErrorCodes.TwoFactorNotInitiated);
        }

        if (!TotpHelper.ValidateCode(user.TwoFactorSecret, request.Code))
        {
            return FluentResult<Response>.ValidationProblem(
                nameof(request.Code),
                IdentityErrorCodes.InvalidTwoFactorSetupCode);
        }

        return await db.ExecuteInTransactionAsync(async () =>
        {
            user.TwoFactorEnabled = true;
            db.Users.Update(user);

            await db.UserRecoveryCodes
                .Where(r => r.UserId == user.Id)
                .ExecuteDeleteAsync(ct);

            var rawCodes = TotpHelper.GenerateRecoveryCodes();

            await db.UserRecoveryCodes.AddRangeAsync(
                rawCodes.Select(rawCode => new UserRecoveryCode
                {
                    UserId = user.Id,
                    CodeHash = TokenHasher.HashToken(rawCode)
                }), ct);

            await db.SaveChangesAsync(ct);

            return FluentResult<Response>.Success(new Response
            {
                RecoveryCodes = rawCodes
            });
        }, ct);
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
        [JsonPropertyName("code")] public required string Code { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("recoveryCodes")] public required string[] RecoveryCodes { get; set; }
    }
}
