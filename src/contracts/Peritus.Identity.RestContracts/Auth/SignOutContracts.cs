using System.Text.Json.Serialization;
using Peritus.Types.Tokens;

namespace Peritus.Identity.RestContracts.Auth;

public sealed class SignOutRequest
{
    [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; init; }
}
