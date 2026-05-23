using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.RestContracts.Auth;

public sealed class EmailConfirmationRequest
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
}

public sealed class ConfirmEmailRequest
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
    [JsonPropertyName("token")] public required string Token { get; init; }
}
