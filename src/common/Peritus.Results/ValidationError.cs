using System.Text.Json.Serialization;

namespace Peritus.FluentResults;

public sealed record ValidationError(string Field, string Code, string Message)
{
    public ValidationError(string field, ErrorCode error) : this(field, error.Code, error.Message)
    {
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Args { get; init; }
}
