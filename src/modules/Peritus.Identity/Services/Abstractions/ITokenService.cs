using System.Security.Claims;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Services.Abstractions;

public interface ITokenService
{
    Task<ClaimsPrincipal> GetPrincipalFromExpiredTokenAsync(AccessToken token);
    Task<AuthTokenPair> GenerateTokensAsync(UserId userId, AccessTokenId accessTokenId, CancellationToken ct = default);
}
