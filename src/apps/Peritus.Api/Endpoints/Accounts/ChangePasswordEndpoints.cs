using System.Security.Claims;
using Peritus.Api.Extensions;
using Peritus.Guard.Extensions;
using Peritus.Identity.Features.Accounts;
using Peritus.ApiContracts.Identity.Accounts;

namespace Peritus.Api.Endpoints.Accounts;

public static class ChangePasswordEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/accounts")
            .WithTags("Passwords")
            .RequireAuthorization();

        group.MapPost("/passwords/change", HandleChangePasswordAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Change password");
    }

    private static async Task<IResult> HandleChangePasswordAsync(
        ChangePasswordRequest request,
        PasswordChangeFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new PasswordChangeFeature.Context
            {
                UserId = principal.GetUserId(),
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            }, ct);

        return result.ToResult();
    }
}
