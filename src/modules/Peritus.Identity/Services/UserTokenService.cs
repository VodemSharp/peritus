using Peritus.Cache.Distributed;
using Peritus.FluentResults;
using Peritus.Identity.Helpers;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Services;

public class UserTokenService(
    IDistributedCacheService cache,
    TimeProvider timeProvider
) : IUserTokenService
{
    public async Task<UserTokenCreationResult> CreateAsync(
        UserId userId,
        UserTokenType type,
        TimeSpan expiry,
        string? value = null,
        CancellationToken ct = default)
    {
        var rawToken = SecureToken.Generate().Value;
        var tokenHash = TokenHasher.HashToken(rawToken);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var info = new UserTokenInfo
        {
            UserId = userId,
            Type = type,
            TokenHash = tokenHash,
            Value = value,
            ExpiresAt = utcNow.Add(expiry),
            CreatedAt = utcNow
        };

        var key = IdentityCacheKeys.UserToken(userId, type, tokenHash);

        await cache.SetAsync(
            key,
            info,
            expiry,
            ct);

        return new UserTokenCreationResult
        {
            Token = info,
            RawToken = rawToken
        };
    }

    public async Task<FluentResult<UserTokenInfo>> RedeemAsync(
        UserId userId,
        UserTokenType type,
        string rawToken,
        CancellationToken ct = default)
    {
        var tokenHash = TokenHasher.HashToken(rawToken);
        var key = IdentityCacheKeys.UserToken(userId, type, tokenHash);

        var info = await cache.GetAsync<UserTokenInfo>(key, ct);
        if (info is null)
        {
            return FluentResult<UserTokenInfo>.ValidationMessage("Invalid or expired token.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (info.ExpiresAt < utcNow)
        {
            await cache.RemoveAsync(key, ct);
            return FluentResult<UserTokenInfo>.ValidationMessage("Token has expired.");
        }

        await cache.RemoveAsync(key, ct);

        return FluentResult<UserTokenInfo>.Success(info);
    }
}
