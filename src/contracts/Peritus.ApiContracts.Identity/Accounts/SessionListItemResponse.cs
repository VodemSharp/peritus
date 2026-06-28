using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Accounts;

public sealed class SessionListItemResponse
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("userAgent")] public required string UserAgent { get; init; }
    [JsonPropertyName("ipAddress")] public string? IpAddress { get; init; }
    [JsonPropertyName("expiredAt")] public required DateTime ExpiredAt { get; init; }
    [JsonPropertyName("createdAt")] public required DateTime CreatedAt { get; init; }
}
