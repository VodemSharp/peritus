using System.Security.Claims;
using Peritus.Api.Extensions;
using Peritus.Guard.Extensions;
using Peritus.Identity.Features.Accounts;
using Peritus.Identity.RestContracts.Accounts;

namespace Peritus.Api.Endpoints.Accounts;

public static class TwoFactorEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/accounts/2fa")
            .WithTags("TwoFactor")
            .RequireAuthorization();

        group.MapPost("/enable", HandleEnableTwoFactorAsync)
            .Produces<EnableTwoFactorResponse>()
            .WithSummary("Enable two-factor authentication");

        group.MapPost("/confirm", HandleConfirmTwoFactorAsync)
            .Produces<VerifyTwoFactorResponse>()
            .WithSummary("Confirm two-factor setup");

        group.MapPost("/disable", HandleDisableTwoFactorAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Disable two-factor authentication");

        group.MapPost("/recovery-codes", HandleGenerateRecoveryCodesAsync)
            .Produces<VerifyTwoFactorResponse>()
            .WithSummary("Generate recovery codes");
    }

    private static async Task<IResult> HandleEnableTwoFactorAsync(
        TwoFactorEnableFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new TwoFactorEnableFeature.Context
            {
                UserId = principal.GetUserId()
            }, ct);

        return result.ToResult(x =>
            new EnableTwoFactorResponse
            {
                Secret = x.Secret,
                QrCodeUri = x.QrCodeUri
            }
        );
    }

    private static async Task<IResult> HandleConfirmTwoFactorAsync(
        VerifyTwoFactorRequest request,
        TwoFactorVerifySetupFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new TwoFactorVerifySetupFeature.Context
            {
                UserId = principal.GetUserId(),
                Code = request.Code
            }, ct);

        return result.ToResult(x =>
            new VerifyTwoFactorResponse
            {
                RecoveryCodes = x.RecoveryCodes
            }
        );
    }

    private static async Task<IResult> HandleDisableTwoFactorAsync(
        TwoFactorDisableFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new TwoFactorDisableFeature.Context
            {
                UserId = principal.GetUserId()
            }, ct);

        return result.ToResult();
    }

    private static async Task<IResult> HandleGenerateRecoveryCodesAsync(
        TwoFactorGenerateRecoveryCodesFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new TwoFactorGenerateRecoveryCodesFeature.Context
            {
                UserId = principal.GetUserId()
            }, ct);

        return result.ToResult(x =>
            new VerifyTwoFactorResponse
            {
                RecoveryCodes = x.RecoveryCodes
            }
        );
    }
}
