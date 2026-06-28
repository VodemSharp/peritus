using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Accounts;

public sealed class TwoFactorVerifySetupRequest
{
    [JsonPropertyName("code")] public required string Code { get; init; }
}
