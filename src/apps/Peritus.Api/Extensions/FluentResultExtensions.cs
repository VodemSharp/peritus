using System.Net;
using Peritus.FluentResults;

namespace Peritus.Api.Extensions;

public static class FluentResultExtensions
{
    public static IResult ToResult<TResult, TResponse>(this FluentResult<TResult> result,
        Func<TResult, TResponse>? responseMap = null, HttpStatusCode successStatusCode = HttpStatusCode.OK)
    {
        if (result.IsSuccess)
        {
            return responseMap is null
                ? GetSuccessResult(result.Result, successStatusCode)
                : GetSuccessResult(responseMap.Invoke(result.Result), successStatusCode);
        }

        return GetFailResult(result.Error);
    }

    public static IResult ToResult(this FluentResult result, HttpStatusCode successStatusCode = HttpStatusCode.OK)
    {
        return result.IsSuccess ? GetSuccessResult(successStatusCode) : GetFailResult(result.Error);
    }

    private static IResult GetFailResult(IFluentResultError error)
    {
        return error switch
        {
            FluentNotFoundResult notFound => Results.NotFound(notFound.Detail),
            FluentValidationProblemResult validationProblem
                => Results.ValidationProblem(validationProblem.Errors),
            FluentValidationMessageResult validationMessage
                => Results.Problem(validationMessage.Message, statusCode: StatusCodes.Status400BadRequest),
            FluentInternalErrorResult internalError => Results.InternalServerError(internalError.Detail),
            _ => throw new ArgumentException($"Not supported error type: {error.GetType().Name}")
        };
    }

    private static IResult GetSuccessResult<T>(T value, HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.OK => Results.Ok(value),
            HttpStatusCode.Created => Results.Created(string.Empty, value),
            _ => GetCommonSuccessResult(statusCode)
        };
    }

    private static IResult GetSuccessResult(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.OK => Results.Ok(),
            HttpStatusCode.Created => Results.Created(string.Empty, null),
            _ => GetCommonSuccessResult(statusCode)
        };
    }

    private static IResult GetCommonSuccessResult(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Accepted => Results.Accepted(),
            HttpStatusCode.NoContent => Results.NoContent(),
            _ => Results.StatusCode((int)statusCode)
        };
    }
}
