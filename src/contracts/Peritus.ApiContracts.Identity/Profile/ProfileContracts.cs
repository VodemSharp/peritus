using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Profile;

public sealed class GetCurrentUserResponse
{
    [JsonPropertyName("email")] public required string Email { get; init; }
}

public sealed class UpdateProfileRequest
{
    [JsonPropertyName("culture")] public string? Culture { get; init; }
}
