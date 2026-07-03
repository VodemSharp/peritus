using System.Text.Json.Serialization;

namespace Peritus.ApiClients.Models;

public sealed class ApiValidationError
{
    [JsonPropertyName("field")] public string? Field { get; set; }
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("args")] public IReadOnlyList<string>? Args { get; set; }
}
