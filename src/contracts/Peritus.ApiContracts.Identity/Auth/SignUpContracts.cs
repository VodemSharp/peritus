using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class SignUpRequest
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
    [JsonPropertyName("password")] public required Password Password { get; init; }
}

public sealed class SignUpResponse
{
    [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; init; }
    [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; init; }
}
