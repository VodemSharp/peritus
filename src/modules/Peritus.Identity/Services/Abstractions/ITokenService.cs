using System.Security.Claims;
using Peritus.FluentResults;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Services.Abstractions;

public interface ITokenService
{
    Task<FluentResult<ClaimsPrincipal>> GetPrincipalFromExpiredTokenAsync(AccessToken token);
    Task<AuthTokenPair> GenerateTokensAsync(UserId userId, AccessTokenId accessTokenId, CancellationToken ct = default);
    Task<string> GenerateTwoFactorTokenAsync(UserId userId, CancellationToken ct = default);
    Task<FluentResult<UserId>> ValidateTwoFactorTokenAsync(string token, CancellationToken ct = default);
}
