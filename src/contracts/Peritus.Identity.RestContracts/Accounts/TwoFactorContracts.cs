using System.Text.Json.Serialization;

namespace Peritus.Identity.RestContracts.Accounts;

public sealed class EnableTwoFactorResponse
{
    [JsonPropertyName("secret")] public required string Secret { get; init; }
    [JsonPropertyName("qrCodeUri")] public required string QrCodeUri { get; init; }
}

public sealed class VerifyTwoFactorRequest
{
    [JsonPropertyName("code")] public required string Code { get; init; }
}

public sealed class VerifyTwoFactorResponse
{
    [JsonPropertyName("recoveryCodes")] public required string[] RecoveryCodes { get; init; }
}
