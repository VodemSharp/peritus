using System.Text.Json.Serialization;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.RestContracts.Users;

public sealed class GetCurrentUserResponse
{
    [JsonPropertyName("email")] public required Email Email { get; init; }
}
