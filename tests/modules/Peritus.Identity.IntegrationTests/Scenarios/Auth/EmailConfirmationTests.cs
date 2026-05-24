using System.Net;
using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.IntegrationTests.Types;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class EmailConfirmationTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SignUp_SendsEmailConfirmationTokenAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = UserCredentials.Create();

        // Act
        await api.SignUpAsync(new SignUpRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        await api.SendEmailConfirmationAsync(new EmailConfirmationRequest
        {
            Email = credentials.Email
        }, _ct);

        // Assert
        Assert.NotNull(TokenCapture.Get(UserTokenType.EmailConfirmation));

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        Assert.False(user.EmailConfirmed);
    }

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

        await api.SendEmailConfirmationAsync(new EmailConfirmationRequest
        {
            Email = credentials.Email
        }, _ct);

        var rawToken = TokenCapture.Get(UserTokenType.EmailConfirmation)!;
        Assert.NotNull(rawToken);

        // Act
        var response = await api.ConfirmEmailAsync(new ConfirmEmailRequest
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
        var response = await api.ConfirmEmailAsync(new ConfirmEmailRequest
        {
            Email = credentials.Email,
            Token = "invalid-token"
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, "Token", "Invalid or expired token.");
    }

    [Fact]
    public async Task ResendEmailConfirmation_GeneratesDifferentTokenAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = UserCredentials.Create();

        await api.SignUpAsync(new SignUpRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        await api.SendEmailConfirmationAsync(new EmailConfirmationRequest
        {
            Email = credentials.Email
        }, _ct);

        var firstToken = TokenCapture.Get(UserTokenType.EmailConfirmation)!;

        // Act
        await api.SendEmailConfirmationAsync(new EmailConfirmationRequest
        {
            Email = credentials.Email
        }, _ct);

        var secondToken = TokenCapture.Get(UserTokenType.EmailConfirmation)!;

        // Assert
        Assert.NotEqual(firstToken, secondToken);
    }
}
