namespace Peritus.FluentResults;

public interface IFluentResultError;

public class FluentNotFoundResult : IFluentResultError
{
    public string? Detail { get; init; }
}

public class FluentValidationProblemResult : IFluentResultError
{
    public required Dictionary<string, string[]> Errors { get; init; }
}

public class FluentInternalErrorResult : IFluentResultError
{
    public string? Detail { get; init; }
}
