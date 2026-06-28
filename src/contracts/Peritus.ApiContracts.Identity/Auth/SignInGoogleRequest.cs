using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class SignInGoogleRequest
{
    [JsonPropertyName("idToken")] public required string IdToken { get; init; }
}
