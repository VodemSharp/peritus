using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Profile;

public sealed class ProfileUpdateRequest
{
    [JsonPropertyName("culture")] public string? Culture { get; init; }
}
