using Peritus.Types.Tokens;

namespace Peritus.Guard.Services.Abstractions;

public interface ITokenRevoker
{
    Task<bool> IsRevokedAsync(AccessTokenId tokenId, CancellationToken ct = default);
    Task RevokeAsync(AccessTokenId tokenId, TimeSpan expiration, CancellationToken ct = default);
}
