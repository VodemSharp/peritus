using System.Text.Json.Serialization;
using Peritus.Types.Tokens;

namespace Peritus.Identity.RestContracts.Auth;

public sealed class RecoveryCodeSignInRequest
{
    [JsonPropertyName("twoFactorToken")] public required string TwoFactorToken { get; init; }
    [JsonPropertyName("code")] public required string Code { get; init; }
}

public sealed class RecoveryCodeSignInResponse
{
    [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; init; }
    [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; init; }
}
