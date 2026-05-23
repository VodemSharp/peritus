using Peritus.FluentResults;
using Peritus.Identity.Services.Abstractions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Localization;

namespace Peritus.Identity.Features.Profile;

public class ProfileUpdateFeature(IUserService userService)
{
    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(context.UserId, ct);

        if (context.Culture is not null)
        {
            user.Culture = context.Culture.Value.Code;
        }

        await userService.UpdateAsync(user, ct);

        return FluentResult.Success();
    }

    public class Context
    {
        public required UserId UserId { get; set; }
        public Culture? Culture { get; set; }
    }
}
