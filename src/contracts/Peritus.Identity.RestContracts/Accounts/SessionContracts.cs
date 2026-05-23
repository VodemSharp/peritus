using System.Text.Json.Serialization;

namespace Peritus.Identity.RestContracts.Accounts;

public sealed class SessionResponse
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("userAgent")] public required string UserAgent { get; init; }
    [JsonPropertyName("ipAddress")] public string? IpAddress { get; init; }
    [JsonPropertyName("expiredAt")] public required DateTime ExpiredAt { get; init; }
}
