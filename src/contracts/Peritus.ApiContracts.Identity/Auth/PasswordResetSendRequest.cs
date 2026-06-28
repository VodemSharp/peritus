using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.ApiContracts.Identity.Auth;

public sealed class PasswordResetSendRequest
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
}
