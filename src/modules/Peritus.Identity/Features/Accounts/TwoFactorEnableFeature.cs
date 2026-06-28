using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Helpers;
using Peritus.Identity.Services.Abstractions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class TwoFactorEnableFeature(
    IUserService userService,
    IOptions<IdentityOptions> identityOptions)
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/accounts/2fa/enable", async (
                TwoFactorEnableFeature feature,
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
            .Produces<Response>()
            .WithTags("TwoFactor");
    }

    private async Task<FluentResult<Response>> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(request.UserId, ct);
        var secret = TotpHelper.GenerateSecret();
        user.TwoFactorSecret = secret;

        await userService.UpdateAsync(user, ct);

        var qrCodeUri = TotpHelper.GenerateQrCodeUri(user.Email, secret, identityOptions.Value.JwtIssuer);

        return FluentResult<Response>.Success(new Response
        {
            Secret = secret,
            QrCodeUri = qrCodeUri
        });
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("secret")] public required string Secret { get; set; }
        [JsonPropertyName("qrCodeUri")] public required string QrCodeUri { get; set; }
    }
}
