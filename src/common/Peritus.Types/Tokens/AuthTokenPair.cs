namespace Peritus.Types.Tokens;

public readonly record struct AuthTokenPair(AccessToken AccessToken, RefreshToken RefreshToken);
