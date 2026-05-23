using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.RestContracts.Accounts;

public sealed class ChangePasswordRequest
{
    [JsonPropertyName("currentPassword")] public required Password CurrentPassword { get; init; }
    [JsonPropertyName("newPassword")] public required Password NewPassword { get; init; }
}
