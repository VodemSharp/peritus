using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class SignUpRequest
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
    [JsonPropertyName("password")] public required Password Password { get; init; }
}
