using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.ApiContracts.Identity.Accounts;

public sealed class PhoneNumberVerificationRequest
{
    [JsonPropertyName("phoneNumber")] public required PhoneNumber PhoneNumber { get; init; }
}

public sealed class PhoneNumberVerifyRequest
{
    [JsonPropertyName("code")] public required string Code { get; init; }
}
