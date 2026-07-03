using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class PhoneNumberVerifyFeature(
    IUserService userService,
    IUserTokenService userTokenService)
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/accounts/phones/verify", async (
                Request request,
                PhoneNumberVerifyFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                request.UserId = principal.GetUserId();

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .WithTags("Phones");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(request.UserId, ct);
        var result = await userTokenService.RedeemAsync(
            user.Id, UserTokenType.PhoneNumberVerification, request.Code, ct);

        if (!result.IsSuccess)
        {
            return FluentResult.ValidationProblem(
                nameof(request.Code),
                IdentityErrorCodes.InvalidOrExpiredPhoneCode);
        }

        var token = result.Result;

        if (string.IsNullOrEmpty(token.Value))
        {
            return FluentResult.InternalError(
                ErrorCodes.Internal,
                new InvalidOperationException("Phone number verification token has no value."));
        }

        user.PhoneNumber = new PhoneNumber(token.Value!);
        user.PhoneNumberConfirmed = true;
        await userService.UpdateAsync(user, ct);

        return FluentResult.Success();
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
        [JsonPropertyName("code")] public required string Code { get; set; }
    }
}
