using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Peritus.FluentResults;

namespace Peritus.AspNetCore.HttpResults;

internal sealed partial class InternalErrorHttpResult(FluentInternalErrorResult error) : IResult
{
    private const string NoDetail = "(no detail)";

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<InternalErrorHttpResult>();

        LogInternalError(
            logger,
            error.Exception,
            error.Code,
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            error.Detail ?? NoDetail);

        await Results.Problem(
            error.Detail,
            statusCode: StatusCodes.Status500InternalServerError,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code
            }).ExecuteAsync(httpContext);
    }

    [LoggerMessage(LogLevel.Error, "Internal error {Code} on {Method} {Path}: {Detail}")]
    private static partial void LogInternalError(
        ILogger logger, Exception? exception, string code, string method, string? path, string detail);
}
