using System.Net;
using Microsoft.AspNetCore.Http;
using Peritus.AspNetCore.HttpResults;
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
            FluentValidationProblemResult validationProblem => Results.Problem(
                title: "One or more validation errors occurred.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = validationProblem.Code,
                    ["errors"] = validationProblem.Errors
                }),
            FluentValidationMessageResult validationMessage => Results.Problem(
                validationMessage.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: BuildExtensions(validationMessage.Code, validationMessage.Args)),
            FluentNotFoundResult notFound => Results.Problem(
                notFound.Detail,
                statusCode: StatusCodes.Status404NotFound,
                extensions: BuildExtensions(notFound.Code, notFound.Args)),
            FluentInternalErrorResult internalError => new InternalErrorHttpResult(internalError),
            _ => throw new ArgumentException($"Not supported error type: {error.GetType().Name}")
        };
    }

    private static Dictionary<string, object?> BuildExtensions(string code, IReadOnlyList<string>? args)
    {
        var extensions = new Dictionary<string, object?>
        {
            ["code"] = code
        };

        if (args is not null)
        {
            extensions["args"] = args;
        }

        return extensions;
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
