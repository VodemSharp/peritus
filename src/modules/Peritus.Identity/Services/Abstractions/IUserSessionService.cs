using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Services.Abstractions;

public interface IUserSessionService
{
    Task<AuthTokenPair> CreateAsync(UserId userId, IpAddress? ip, UserAgent userAgent,
        UserSessionProvider tokensProvider, CancellationToken ct = default);
}
