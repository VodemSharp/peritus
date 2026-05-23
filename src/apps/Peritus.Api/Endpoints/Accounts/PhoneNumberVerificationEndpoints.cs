using System.Security.Claims;
using Peritus.Api.Extensions;
using Peritus.Guard.Extensions;
using Peritus.Identity.Features.Accounts;
using Peritus.ApiContracts.Identity.Accounts;

namespace Peritus.Api.Endpoints.Accounts;

public static class PhoneNumberVerificationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/accounts/phones")
            .WithTags("Phones")
            .RequireAuthorization();

        group.MapPost("/send-verification", HandleSendPhoneNumberVerificationAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Send phone number verification");

        group.MapPost("/verify", HandleConfirmPhoneNumberAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Confirm phone number");
    }

    private static async Task<IResult> HandleSendPhoneNumberVerificationAsync(
        PhoneNumberVerificationRequest request,
        PhoneNumberSendVerificationFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new PhoneNumberSendVerificationFeature.Context
            {
                UserId = principal.GetUserId(),
                PhoneNumber = request.PhoneNumber
            }, ct);

        return result.ToResult();
    }

    private static async Task<IResult> HandleConfirmPhoneNumberAsync(
        PhoneNumberVerifyRequest request,
        PhoneNumberVerifyFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new PhoneNumberVerifyFeature.Context
            {
                UserId = principal.GetUserId(),
                Code = request.Code
            }, ct);

        return result.ToResult();
    }
}
