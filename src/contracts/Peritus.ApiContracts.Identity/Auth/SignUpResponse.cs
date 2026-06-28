using System.Text.Json.Serialization;
using Peritus.Types.Tokens;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class SignUpResponse
{
    [JsonPropertyName("accessToken")] public AccessToken? AccessToken { get; init; }
    [JsonPropertyName("refreshToken")] public RefreshToken? RefreshToken { get; init; }

    [JsonPropertyName("emailConfirmationRequired")]
    public required bool EmailConfirmationRequired { get; init; }
}
