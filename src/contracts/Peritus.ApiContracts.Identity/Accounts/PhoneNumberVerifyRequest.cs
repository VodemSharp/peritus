using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Accounts;

public sealed class PhoneNumberVerifyRequest
{
    [JsonPropertyName("code")] public required string Code { get; init; }
}
