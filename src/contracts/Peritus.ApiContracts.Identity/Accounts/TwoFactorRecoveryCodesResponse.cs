using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Accounts;

public sealed class TwoFactorRecoveryCodesResponse
{
    [JsonPropertyName("recoveryCodes")] public required string[] RecoveryCodes { get; init; }
}
