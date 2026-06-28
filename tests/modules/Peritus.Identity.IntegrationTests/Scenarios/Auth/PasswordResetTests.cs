using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class PasswordResetTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesPasswordAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        await api.SendPasswordResetAsync(new PasswordResetSendRequest
        {
            Email = credentials.Email
        }, _ct);

        var rawToken = TokenCapture.Get(UserTokenType.PasswordReset)!;
        Assert.NotNull(rawToken);

        var newPassword = new Password("NewPassword123!");

        // Act
        var response = await api.ResetPasswordAsync(new PasswordResetRequest
        {
            Email = credentials.Email,
            Token = rawToken,
            NewPassword = newPassword
        }, _ct);

        // Assert
        Assert.True(response.IsSuccessful);

        var signInResponse = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = newPassword
        }, _ct);

        ApiAssert.Success(signInResponse);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_FailsAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        // Act
        var response = await api.ResetPasswordAsync(new PasswordResetRequest
        {
            Email = credentials.Email,
            Token = "invalid-token",
            NewPassword = new Password("NewPassword123!")
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, "Token", "Invalid or expired token.");
    }

    [Fact]
    public async Task ResetPassword_OldPasswordNoLongerWorksAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        await api.SendPasswordResetAsync(new PasswordResetSendRequest
        {
            Email = credentials.Email
        }, _ct);

        var rawToken = TokenCapture.Get(UserTokenType.PasswordReset)!;
        Assert.NotNull(rawToken);

        await api.ResetPasswordAsync(new PasswordResetRequest
        {
            Email = credentials.Email,
            Token = rawToken,
            NewPassword = new Password("NewPassword123!")
        }, _ct);

        // Act
        var signInResponse = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(signInResponse, nameof(SignInRequest.Password), "Invalid credentials.");
    }
}
