using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class SignInRecoveryCodeRequest
{
    [JsonPropertyName("twoFactorToken")] public required string TwoFactorToken { get; init; }
    [JsonPropertyName("code")] public required string Code { get; init; }
}
