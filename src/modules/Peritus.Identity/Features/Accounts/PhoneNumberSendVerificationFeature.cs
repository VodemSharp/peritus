using Microsoft.Extensions.Options;
using Peritus.FluentResults;
using Peritus.Identity.Options;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Notification.Features;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class PhoneNumberSendVerificationFeature(
    IUserService userService,
    IUserTokenService userTokenService,
    SmsSendFeature smsSendFeature,
    IOptions<IdentityOptions> identityOptions)
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(context.UserId, ct);

        if (!PhoneNumber.IsValid(context.PhoneNumber))
        {
            return FluentResult.ValidationProblem(nameof(context.PhoneNumber), "Invalid phone number format.");
        }

        if (user.PhoneNumber == context.PhoneNumber && user.PhoneNumberConfirmed)
        {
            return FluentResult.Success();
        }

        var tokenResult = await userTokenService.CreateAsync(
            user.Id,
            UserTokenType.PhoneNumberVerification,
            _options.PhoneNumberVerificationTokenExpiry,
            context.PhoneNumber,
            ct);

        smsSendFeature.Execute(new SmsSendFeature.Context
        {
            To = context.PhoneNumber,
            Message = $"Your verification code is: {tokenResult.RawToken}"
        });

        return FluentResult.Success();
    }

    public class Context
    {
        public required UserId UserId { get; set; }
        public required PhoneNumber PhoneNumber { get; set; }
    }
}
