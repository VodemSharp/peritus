using System.Text.Json.Serialization;
using Peritus.Types.Tokens;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class TwoFactorSignInRequest
{
    [JsonPropertyName("twoFactorToken")] public required string TwoFactorToken { get; init; }
    [JsonPropertyName("code")] public required string Code { get; init; }
}

public sealed class TwoFactorSignInResponse
{
    [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; init; }
    [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; init; }
}
