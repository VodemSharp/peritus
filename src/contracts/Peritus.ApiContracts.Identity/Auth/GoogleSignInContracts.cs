using System.Text.Json.Serialization;
using Peritus.Types.Tokens;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class GoogleSignInRequest
{
    [JsonPropertyName("idToken")] public required string IdToken { get; init; }
}

public sealed class GoogleSignInResponse
{
    [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; init; }
    [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; init; }
}
