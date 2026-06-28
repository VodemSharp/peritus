using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.ApiContracts.Identity.Accounts;

public sealed class PhoneNumberSendVerificationRequest
{
    [JsonPropertyName("phoneNumber")] public required PhoneNumber PhoneNumber { get; init; }
}
