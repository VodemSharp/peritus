using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.RestContracts.Auth;

public sealed class SignInRequest
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
    [JsonPropertyName("password")] public required Password Password { get; init; }
}

public sealed class SignInResponse
{
    [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; init; }
    [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; init; }
}
