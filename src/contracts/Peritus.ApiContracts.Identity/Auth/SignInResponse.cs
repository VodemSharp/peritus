using System.Text.Json.Serialization;
using Peritus.Types.Tokens;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class SignInResponse
{
    [JsonPropertyName("accessToken")] public AccessToken? AccessToken { get; init; }
    [JsonPropertyName("refreshToken")] public RefreshToken? RefreshToken { get; init; }

    [JsonPropertyName("requiresTwoFactor")]
    public bool RequiresTwoFactor { get; init; }

    [JsonPropertyName("twoFactorToken")] public string? TwoFactorToken { get; init; }
}
