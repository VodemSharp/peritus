using Peritus.FluentResults;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class PhoneNumberVerifyFeature(
    IUserService userService,
    IUserTokenService userTokenService)
{
    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(context.UserId, ct);
        var result = await userTokenService.RedeemAsync(user.Id, UserTokenType.PhoneNumberVerification, context.Code, ct);

        if (!result.IsSuccess)
        {
            return FluentResult.ValidationProblem(nameof(context.Code), "Invalid or expired code.");
        }

        var token = result.Result;

        if (string.IsNullOrEmpty(token.Value))
        {
            throw new InvalidOperationException("Phone number verification token has no value.");
        }

        user.PhoneNumber = new PhoneNumber(token.Value!);
        user.PhoneNumberConfirmed = true;
        await userService.UpdateAsync(user, ct);

        return FluentResult.Success();
    }

    public class Context
    {
        public required UserId UserId { get; set; }
        public required string Code { get; set; }
    }
}
