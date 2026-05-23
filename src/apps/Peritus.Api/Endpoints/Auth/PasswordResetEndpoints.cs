using Peritus.Api.Extensions;
using Peritus.Identity.Features.Auth;
using Peritus.ApiContracts.Identity.Auth;

namespace Peritus.Api.Endpoints.Auth;

public static class PasswordResetEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/auth/passwords")
            .WithTags("Passwords");

        group.MapPost("/forgot", HandleSendPasswordResetAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Send password reset");

        group.MapPost("/reset", HandleResetPasswordAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Reset password");
    }

    private static async Task<IResult> HandleSendPasswordResetAsync(
        SendPasswordResetRequest request,
        PasswordResetSendFeature feature,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new PasswordResetSendFeature.Context
            {
                Email = request.Email
            }, ct);

        return result.ToResult();
    }

    private static async Task<IResult> HandleResetPasswordAsync(
        ResetPasswordRequest request,
        PasswordResetFeature feature,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new PasswordResetFeature.Context
            {
                Email = request.Email,
                Token = request.Token,
                NewPassword = request.NewPassword
            }, ct);

        return result.ToResult();
    }
}
