namespace Peritus.FluentResults;

public interface IFluentResultError
{
    string Code { get; }
}

public class FluentNotFoundResult : IFluentResultError
{
    public string? Detail { get; init; }
    public IReadOnlyList<string>? Args { get; init; }
    public required string Code { get; init; }
}

public class FluentValidationProblemResult : IFluentResultError
{
    public required IReadOnlyList<ValidationError> Errors { get; init; }
    public string Code => ErrorCodes.Validation.Code;
}

public class FluentValidationMessageResult : IFluentResultError
{
    public required string Message { get; init; }
    public IReadOnlyList<string>? Args { get; init; }
    public required string Code { get; init; }
}

public class FluentInternalErrorResult : IFluentResultError
{
    public string? Detail { get; init; }
    public Exception? Exception { get; init; }
    public required string Code { get; init; }
}
