using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Accounts;

public sealed class TwoFactorEnableResponse
{
    [JsonPropertyName("secret")] public required string Secret { get; init; }
    [JsonPropertyName("qrCodeUri")] public required string QrCodeUri { get; init; }
}
