using System.Net;
using Microsoft.AspNetCore.Mvc;
using Refit;
using Xunit;

namespace Peritus.IntegrationTests.Assertions;

public static class ApiAssert
{
    public static void Success<T>(
        IApiResponse<T> response, Action<T> assert, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Assert.Equal(statusCode, response.StatusCode);
        Assert.NotNull(response.Content);
        assert.Invoke(response.Content);
    }

    public static async Task SuccessAsync<T>(
        IApiResponse<T> response, Func<T, Task> assert, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Assert.Equal(statusCode, response.StatusCode);
        Assert.NotNull(response.Content);
        await assert.Invoke(response.Content);
    }

    public static async Task ValidationErrorAsync<T>(IApiResponse<T> response, string fieldName, string expectedError)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(response.Error);

        var problem = await response.Error.GetContentAsAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.TryGetValue(fieldName, out var fieldErrors));
        Assert.Equal(expectedError, fieldErrors[0]);
    }
}
