using Microsoft.Extensions.Options;
using Peritus.FluentResults;
using Peritus.Identity.Helpers;
using Peritus.Identity.Options;
using Peritus.Identity.Services.Abstractions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class TwoFactorEnableFeature(
    IUserService userService,
    IOptions<IdentityOptions> identityOptions)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(context.UserId, ct);
        var secret = TotpHelper.GenerateSecret();
        user.TwoFactorSecret = secret;

        await userService.UpdateAsync(user, ct);

        var qrCodeUri = TotpHelper.GenerateQrCodeUri(user.Email, secret, identityOptions.Value.JwtIssuer);

        return FluentResult<Result>.Success(new Result
        {
            Secret = secret,
            QrCodeUri = qrCodeUri
        });
    }

    public class Context
    {
        public required UserId UserId { get; set; }
    }

    public class Result
    {
        public required string Secret { get; set; }
        public required string QrCodeUri { get; set; }
    }
}
