using Microsoft.Extensions.Options;
using Peritus.FluentResults;
using Peritus.Identity.Options;
using Peritus.Identity.Services.Abstractions;
using Peritus.Notification.Features;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Auth;

public class EmailSendConfirmationFeature(
    IUserService userService,
    IUserTokenService userTokenService,
    EmailSendFeature emailSendFeature,
    IOptions<IdentityOptions> identityOptions
)
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(context.Email, ct);

        if (user is null || user.EmailConfirmed)
        {
            // Return success even if user not found or already confirmed (idempotent, no enumeration)
            return FluentResult.Success();
        }

        var result = await userTokenService.CreateAsync(
            user.Id,
            UserTokenType.EmailConfirmation,
            _options.EmailConfirmationTokenExpiry,
            ct: ct);

        emailSendFeature.Execute(new EmailSendFeature.Context
        {
            To = user.Email.Value,
            Subject = "Confirm your email",
            Body = $"Your confirmation code is: {result.RawToken}"
        });

        return FluentResult.Success();
    }

    public class Context
    {
        public required Email Email { get; set; }
    }
}
