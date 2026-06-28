using System.Net;
using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.IntegrationTests.Types;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class EmailConfirmTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ConfirmsEmailAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = UserCredentials.Create();

        await api.SignUpAsync(new SignUpRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        await api.SendEmailConfirmationAsync(new EmailSendConfirmationRequest
        {
            Email = credentials.Email
        }, _ct);

        var rawToken = TokenCapture.Get(UserTokenType.EmailConfirmation)!;
        Assert.NotNull(rawToken);

        // Act
        var response = await api.ConfirmEmailAsync(new EmailConfirmRequest
        {
            Email = credentials.Email,
            Token = rawToken
        }, _ct);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_FailsAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = UserCredentials.Create();

        await api.SignUpAsync(new SignUpRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        // Act
        var response = await api.ConfirmEmailAsync(new EmailConfirmRequest
        {
            Email = credentials.Email,
            Token = "invalid-token"
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, "Token", "Invalid or expired token.");
    }
}
