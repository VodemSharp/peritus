using Peritus.Cache.Distributed;
using Peritus.FluentResults;
using Peritus.Identity.Services;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.IntegrationTests.Infrastructure;

public class CapturingUserTokenService(IDistributedCacheService cache, TimeProvider timeProvider, TokenCaptureState state)
    : IUserTokenService
{
    private readonly UserTokenService _inner = new(cache, timeProvider);

    public async Task<UserTokenCreationResult> CreateAsync(
        UserId userId,
        UserTokenType type,
        TimeSpan expiry,
        string? value = null,
        CancellationToken ct = default)
    {
        var result = await _inner.CreateAsync(userId, type, expiry, value, ct);
        state.Capture(type, result.RawToken);
        return result;
    }

    public Task<FluentResult<UserTokenInfo>> RedeemAsync(
        UserId userId,
        UserTokenType type,
        string rawToken,
        CancellationToken ct = default)
        => _inner.RedeemAsync(userId, type, rawToken, ct);
}
