using System.Net;
using JetBrains.Annotations;
using Peritus.ApiClients.Models;
using Peritus.FluentResults;
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
    public static void Success(IApiResponse response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Assert.True(response.IsSuccessful, response.Error?.Message);
        Assert.Equal(statusCode, response.StatusCode);
    }

    [AssertionMethod]
    public static async Task ValidationErrorAsync<T>(IApiResponse<T> response, string fieldName, string expectedError)
    {
        await AssertFieldErrorAsync(response, fieldName, null, expectedError);
    }

    [AssertionMethod]
    public static async Task ValidationErrorAsync(IApiResponse response, string fieldName, string expectedError)
    {
        await AssertFieldErrorAsync(response, fieldName, null, expectedError);
    }

    [AssertionMethod]
    public static async Task ValidationErrorAsync<T>(
        IApiResponse<T> response,
        string fieldName,
        string expectedCode,
        string expectedError)
    {
        await AssertFieldErrorAsync(response, fieldName, expectedCode, expectedError);
    }

    [AssertionMethod]
    public static async Task ValidationErrorAsync(
        IApiResponse response,
        string fieldName,
        string expectedCode,
        string expectedError)
    {
        await AssertFieldErrorAsync(response, fieldName, expectedCode, expectedError);
    }

    [AssertionMethod]
    public static async Task ValidationErrorAsync<T>(IApiResponse<T> response, string fieldName, ErrorCode expected)
    {
        await AssertFieldErrorAsync(response, fieldName, expected.Code, expected.Message);
    }

    [AssertionMethod]
    public static async Task ValidationErrorAsync(IApiResponse response, string fieldName, ErrorCode expected)
    {
        await AssertFieldErrorAsync(response, fieldName, expected.Code, expected.Message);
    }

    [AssertionMethod]
    public static async Task ValidationMessageAsync<T>(IApiResponse<T> response, string expectedMessage)
    {
        await AssertMessageAsync(response, null, expectedMessage);
    }

    [AssertionMethod]
    public static async Task ValidationMessageAsync(IApiResponse response, string expectedMessage)
    {
        await AssertMessageAsync(response, null, expectedMessage);
    }

    [AssertionMethod]
    public static async Task ValidationMessageAsync<T>(
        IApiResponse<T> response,
        string expectedCode,
        string expectedMessage)
    {
        await AssertMessageAsync(response, expectedCode, expectedMessage);
    }

    [AssertionMethod]
    public static async Task ValidationMessageAsync(
        IApiResponse response,
        string expectedCode,
        string expectedMessage)
    {
        await AssertMessageAsync(response, expectedCode, expectedMessage);
    }

    [AssertionMethod]
    public static async Task ValidationMessageAsync<T>(IApiResponse<T> response, ErrorCode expected)
    {
        await AssertMessageAsync(response, expected.Code, expected.Message);
    }

    [AssertionMethod]
    public static async Task ValidationMessageAsync(IApiResponse response, ErrorCode expected)
    {
        await AssertMessageAsync(response, expected.Code, expected.Message);
    }

    [AssertionMethod]
    public static async Task InternalErrorAsync<T>(IApiResponse<T> response, string expectedCode)
    {
        await AssertCodeAsync(response, HttpStatusCode.InternalServerError, expectedCode);
    }

    [AssertionMethod]
    public static async Task InternalErrorAsync(IApiResponse response, string expectedCode)
    {
        await AssertCodeAsync(response, HttpStatusCode.InternalServerError, expectedCode);
    }

    [AssertionMethod]
    public static async Task InternalErrorAsync<T>(IApiResponse<T> response, ErrorCode expected)
    {
        await AssertCodeAsync(response, HttpStatusCode.InternalServerError, expected.Code);
    }

    [AssertionMethod]
    public static async Task InternalErrorAsync(IApiResponse response, ErrorCode expected)
    {
        await AssertCodeAsync(response, HttpStatusCode.InternalServerError, expected.Code);
    }

    private static async Task AssertCodeAsync(IApiResponse response, HttpStatusCode statusCode, string expectedCode)
    {
        var problem = await ReadProblemAsync(response, statusCode);
        Assert.Equal(expectedCode, problem.Code);
    }

    private static async Task AssertFieldErrorAsync(
        IApiResponse response,
        string fieldName,
        string? expectedCode,
        string expectedError)
    {
        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);

        Assert.NotNull(problem.Errors);
        var fieldError = problem.Errors.SingleOrDefault(e => e.Field == fieldName);
        Assert.NotNull(fieldError);
        Assert.Equal(expectedError, fieldError.Message);

        if (expectedCode is not null)
        {
            Assert.Equal(expectedCode, fieldError.Code);
        }
    }

    private static async Task AssertMessageAsync(IApiResponse response, string? expectedCode, string expectedMessage)
    {
        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);

        Assert.Equal(expectedMessage, problem.Detail);

        if (expectedCode is not null)
        {
            Assert.Equal(expectedCode, problem.Code);
        }
    }

    private static async Task<ApiErrorResponse> ReadProblemAsync(IApiResponse response, HttpStatusCode statusCode)
    {
        Assert.True(response.HasResponseError(out var apiException), response.Error?.Message);
        Assert.NotNull(apiException);
        Assert.Equal(statusCode, apiException.StatusCode);

        var problem = await apiException.GetContentAsAsync<ApiErrorResponse>();
        Assert.NotNull(problem);
        return problem;
    }
}
