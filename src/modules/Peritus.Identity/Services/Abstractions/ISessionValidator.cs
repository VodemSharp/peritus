using Peritus.Types.Tokens;

namespace Peritus.Identity.Services.Abstractions;

public interface ISessionValidator
{
    Task<bool> IsValidAsync(AccessTokenId accessTokenId, CancellationToken ct = default);
    Task SetAsync(AccessTokenId accessTokenId, DateTime expiredAt, CancellationToken ct = default);
    Task RemoveAsync(AccessTokenId accessTokenId, CancellationToken ct = default);
}
