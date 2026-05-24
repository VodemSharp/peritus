using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Services.Abstractions;

public interface IUserSessionService
{
    Task<SessionCreationResult> CreateAsync(UserId userId, IpAddress? ip, UserAgent userAgent,
        UserExternalLoginId? externalLoginId = null, CancellationToken ct = default);

    Task<UserSession?> FindByAccessTokenIdAsync(AccessTokenId accessTokenId, CancellationToken ct = default);
}

public readonly record struct SessionCreationResult(
    AccessTokenId AccessTokenId,
    AuthTokenPair Tokens,
    DateTime ExpiredAt);
