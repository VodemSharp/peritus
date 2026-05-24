using System.Text.Json;
using System.Text.Json.Serialization;

namespace Peritus.ApiClients.Models;

public sealed class ApiErrorResponse
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("errors")] public Dictionary<string, string[]>? Errors { get; set; }

    public static string? Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(content);
            if (error?.Errors is { Count: > 0 } errors)
            {
                return string.Join(" ", errors.SelectMany(e => e.Value));
            }

            return error?.Title;
        }
        catch
        {
            return content;
        }
    }
}
