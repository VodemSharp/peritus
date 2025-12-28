namespace Peritus.FluentResults;

public class FluentResult
{
    protected FluentResult(bool isSuccess, IFluentResultError? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public IFluentResultError? Error { get; }

    public static FluentResult Success()
    {
        return new FluentResult(true);
    }

    public static FluentResult ValidationProblem(string field, string message)
    {
        return new FluentResult(false, new FluentValidationProblemResult
        {
            Errors = new Dictionary<string, string[]>
            {
                {
                    field, [message]
                }
            }
        });
    }

    public static FluentResult ValidationProblem(Dictionary<string, string[]> errors)
    {
        return new FluentResult(false, new FluentValidationProblemResult
        {
            Errors = errors
        });
    }

    public static FluentResult NotFound(string? detail = null)
    {
        return new FluentResult(false, new FluentNotFoundResult
        {
            Detail = detail
        });
    }

    public static FluentResult InternalError(string? detail = null)
    {
        return new FluentResult(false, new FluentInternalErrorResult
        {
            Detail = detail
        });
    }
}

public sealed class FluentResult<TResult> : FluentResult
{
    private FluentResult(bool isSuccess, TResult? result, IFluentResultError? error = null) : base(isSuccess, error)
    {
        Result = result;
    }

    public TResult? Result { get; }

    public static FluentResult<TResult> Success(TResult result)
    {
        return new FluentResult<TResult>(true, result);
    }

    public static new FluentResult<TResult> ValidationProblem(string field, string message)
    {
        return new FluentResult<TResult>(false, default, new FluentValidationProblemResult
        {
            Errors = new Dictionary<string, string[]>
            {
                {
                    field, [message]
                }
            }
        });
    }

    public static new FluentResult<TResult> ValidationProblem(Dictionary<string, string[]> errors)
    {
        return new FluentResult<TResult>(false, default, new FluentValidationProblemResult
        {
            Errors = errors
        });
    }

    public static new FluentResult<TResult> NotFound(string? detail = null)
    {
        return new FluentResult<TResult>(false, default, new FluentNotFoundResult
        {
            Detail = detail
        });
    }

    public static new FluentResult<TResult> InternalError(string? detail = null)
    {
        return new FluentResult<TResult>(false, default, new FluentInternalErrorResult
        {
            Detail = detail
        });
    }
}
