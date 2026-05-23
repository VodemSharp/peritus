using Peritus.Cache.Distributed;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity;

internal static class IdentityCacheKeys
{
    private const string Prefix = "identity";

    public static DistributedCacheKey Session(AccessTokenId accessTokenId)
    {
        return Prefixed($"session:{accessTokenId.Value}");
    }

    public static DistributedCacheKey AuthFailed(UserId userId)
    {
        return Prefixed($"auth:failed:{userId.Value}");
    }

    public static DistributedCacheKey AuthLockout(UserId userId)
    {
        return Prefixed($"auth:lockout:{userId.Value}");
    }

    public static DistributedCacheKey UserToken(UserId userId, UserTokenType type, string tokenHash)
    {
        return Prefixed($"token:{userId.Value}:{type.Value}:{tokenHash}");
    }

    public static DistributedCacheKey TwoFactorToken(string token)
    {
        return Prefixed($"2fa:{token}");
    }

    private static DistributedCacheKey Prefixed(string key)
    {
        return DistributedCacheKey.Create($"{Prefix}:{key}");
    }
}
