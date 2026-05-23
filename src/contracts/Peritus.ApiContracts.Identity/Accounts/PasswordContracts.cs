using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.ApiContracts.Identity.Accounts;

public sealed class ChangePasswordRequest
{
    [JsonPropertyName("currentPassword")] public required Password CurrentPassword { get; init; }
    [JsonPropertyName("newPassword")] public required Password NewPassword { get; init; }
}
