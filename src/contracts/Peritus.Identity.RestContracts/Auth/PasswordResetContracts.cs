using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.RestContracts.Auth;

public sealed class SendPasswordResetRequest
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
}

public sealed class ResetPasswordRequest
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
    [JsonPropertyName("token")] public required string Token { get; init; }
    [JsonPropertyName("newPassword")] public required Password NewPassword { get; init; }
}
