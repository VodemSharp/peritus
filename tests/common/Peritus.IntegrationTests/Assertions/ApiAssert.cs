using System.Net;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Refit;
using Xunit;

namespace Peritus.IntegrationTests.Assertions;

public static class ApiAssert
{
    [AssertionMethod]
    public static T Success<T>(IApiResponse<T> response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Assert.True(response.IsSuccessful, response.Error?.Message);
        Assert.Equal(statusCode, response.StatusCode);
        Assert.NotNull(response.Content);
        return response.Content;
    }

    [AssertionMethod]
    public static async Task ValidationErrorAsync<T>(IApiResponse<T> response, string fieldName, string expectedError)
    {
        Assert.True(response.HasResponseError(out var apiException), response.Error?.Message);
        Assert.Equal(HttpStatusCode.BadRequest, apiException.StatusCode);

        var problem = await apiException.GetContentAsAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.TryGetValue(fieldName, out var fieldErrors));
        Assert.Equal(expectedError, fieldErrors[0]);
    }

    [AssertionMethod]
    public static async Task ValidationErrorAsync(IApiResponse response, string fieldName, string expectedError)
    {
        Assert.True(response.HasResponseError(out var apiException), response.Error?.Message);
        Assert.Equal(HttpStatusCode.BadRequest, apiException.StatusCode);

        var problem = await apiException.GetContentAsAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.TryGetValue(fieldName, out var fieldErrors));
        Assert.Equal(expectedError, fieldErrors[0]);
    }
}
