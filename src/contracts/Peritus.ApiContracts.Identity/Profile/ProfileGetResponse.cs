using System.Text.Json.Serialization;

namespace Peritus.ApiContracts.Identity.Profile;

public sealed class ProfileGetResponse
{
    [JsonPropertyName("email")] public required string Email { get; init; }
}
