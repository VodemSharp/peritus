using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class EmailConfirmRequest
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
    [JsonPropertyName("token")] public required string Token { get; init; }
}
