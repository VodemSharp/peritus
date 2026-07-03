using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Peritus.FluentResults;

public class FluentResult
{
    protected FluentResult(bool isSuccess, IFluentResultError? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public virtual bool IsSuccess { get; }

    public IFluentResultError? Error { get; }

    public static FluentResult Success()
    {
        return new FluentResult(true);
    }

    public static FluentResult ValidationProblem(string field, ErrorCode error, params object?[] args)
    {
        var formatted = Format(error, args);
        return new FluentResult(false, new FluentValidationProblemResult
        {
            Errors =
            [
                new ValidationError(field, error.Code, formatted.Message)
                {
                    Args = formatted.Args
                }
            ]
        });
    }

    public static FluentResult ValidationProblem(IReadOnlyList<ValidationError> errors)
    {
        return new FluentResult(false, new FluentValidationProblemResult
        {
            Errors = errors
        });
    }

    public static FluentResult ValidationMessage(ErrorCode error, params object?[] args)
    {
        var formatted = Format(error, args);
        return new FluentResult(false, new FluentValidationMessageResult
        {
            Code = error.Code,
            Message = formatted.Message,
            Args = formatted.Args
        });
    }

    public static FluentResult NotFound(ErrorCode error, params object?[] args)
    {
        var formatted = Format(error, args);
        return new FluentResult(false, new FluentNotFoundResult
        {
            Code = error.Code,
            Detail = formatted.Message,
            Args = formatted.Args
        });
    }

    public static FluentResult InternalError(ErrorCode error, Exception? exception = null)
    {
        return new FluentResult(false, new FluentInternalErrorResult
        {
            Code = error.Code,
            Detail = error.Message,
            Exception = exception
        });
    }

    protected static FormattedError Format(ErrorCode error, object?[] args)
    {
        if (args.Length == 0)
        {
            return new FormattedError(error.Message, null);
        }

        var message = string.Format(CultureInfo.InvariantCulture, error.Message, args);
        var rendered = Array.ConvertAll(args,
            arg => Convert.ToString(arg, CultureInfo.InvariantCulture) ?? string.Empty);
        return new FormattedError(message, rendered);
    }

    protected readonly record struct FormattedError(string Message, IReadOnlyList<string>? Args);
}

public sealed class FluentResult<TResult> : FluentResult
{
    private FluentResult(bool isSuccess, TResult? result, IFluentResultError? error = null) : base(isSuccess, error)
    {
        Result = result;
    }

    [MemberNotNullWhen(true, nameof(Result))]
    public override bool IsSuccess => base.IsSuccess;

    public TResult? Result { get; }

    public static FluentResult<TResult> Success(TResult result)
    {
        return new FluentResult<TResult>(true, result);
    }

    public static new FluentResult<TResult> ValidationProblem(string field, ErrorCode error, params object?[] args)
    {
        var formatted = Format(error, args);
        return new FluentResult<TResult>(false, default, new FluentValidationProblemResult
        {
            Errors =
            [
                new ValidationError(field, error.Code, formatted.Message)
                {
                    Args = formatted.Args
                }
            ]
        });
    }

    public static new FluentResult<TResult> ValidationProblem(IReadOnlyList<ValidationError> errors)
    {
        return new FluentResult<TResult>(false, default, new FluentValidationProblemResult
        {
            Errors = errors
        });
    }

    public static new FluentResult<TResult> ValidationMessage(ErrorCode error, params object?[] args)
    {
        var formatted = Format(error, args);
        return new FluentResult<TResult>(false, default, new FluentValidationMessageResult
        {
            Code = error.Code,
            Message = formatted.Message,
            Args = formatted.Args
        });
    }

    public static new FluentResult<TResult> NotFound(ErrorCode error, params object?[] args)
    {
        var formatted = Format(error, args);
        return new FluentResult<TResult>(false, default, new FluentNotFoundResult
        {
            Code = error.Code,
            Detail = formatted.Message,
            Args = formatted.Args
        });
    }

    public static new FluentResult<TResult> InternalError(ErrorCode error, Exception? exception = null)
    {
        return new FluentResult<TResult>(false, default, new FluentInternalErrorResult
        {
            Code = error.Code,
            Detail = error.Message,
            Exception = exception
        });
    }
}
