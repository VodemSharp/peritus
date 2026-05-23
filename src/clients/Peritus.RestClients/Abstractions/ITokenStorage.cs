using Peritus.Types.Tokens;

namespace Peritus.RestClients.Abstractions;

public interface ITokenStorage
{
    AccessToken? AccessToken { get; }
    RefreshToken? RefreshToken { get; }
    void SetTokens(AccessToken accessToken, RefreshToken refreshToken);
    void ClearTokens();
}
