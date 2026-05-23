using Peritus.FluentResults;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Auth;

public class EmailConfirmFeature(
    IUserService userService,
    IUserTokenService userTokenService
)
{
    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(context.Email, ct);

        if (user is null)
        {
            return FluentResult.ValidationProblem(nameof(context.Token), "Invalid or expired token.");
        }

        var result = await userTokenService.RedeemAsync(user.Id, UserTokenType.EmailConfirmation, context.Token, ct);

        if (!result.IsSuccess)
        {
            return FluentResult.ValidationProblem(nameof(context.Token), "Invalid or expired token.");
        }

        user.EmailConfirmed = true;
        await userService.UpdateAsync(user, ct);

        return FluentResult.Success();
    }

    public class Context
    {
        public required Email Email { get; set; }
        public required string Token { get; set; }
    }
}
