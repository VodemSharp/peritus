using System.Text.Json.Serialization;
using Peritus.Types.Tokens;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class RefreshTokenRequest
{
    [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; init; }
    [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; init; }
}

public sealed class RefreshTokenResponse
{
    [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; init; }
    [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; init; }
}
