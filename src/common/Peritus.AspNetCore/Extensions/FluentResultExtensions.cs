using System.Net;
using Microsoft.AspNetCore.Http;
using Peritus.FluentResults;

namespace Peritus.AspNetCore.Extensions;

public static class FluentResultExtensions
{
    public static IResult ToResult<TResult>(this FluentResult<TResult> result,
        HttpStatusCode successStatusCode = HttpStatusCode.OK)
    {
        return result.IsSuccess
            ? GetSuccessResult(result.Result, successStatusCode)
            : GetFailResult(result.Error);
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
