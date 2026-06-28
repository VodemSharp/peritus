using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Accounts;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.IntegrationTests.Scenarios.Accounts;

public class PasswordChangeTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_TerminatesSessionsAndAllowsNewPasswordAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);

        var newPassword = new Password("NewPassword123!");

        // Act
        var response = await api.ChangePasswordAsync(new PasswordChangeRequest
        {
            CurrentPassword = credentials.Password,
            NewPassword = newPassword
        }, _ct);

        // Assert
        Assert.True(response.IsSuccessful);

        await using var db = CreateIdentityDbContext();
        var sessions = await db.UserSessions
            .Where(s => s.User!.Email == credentials.Email)
            .ToListAsync(_ct);

        Assert.All(sessions, session => Assert.Equal(UserSessionStatus.Terminated, session.Status));

        var anonymousApi = CreateIdentityApi();
        var signInResponse = await anonymousApi.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = newPassword
        }, _ct);

        ApiAssert.Success(signInResponse);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_FailsAsync()
    {
        // Arrange
        var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);

        // Act
        var response = await api.ChangePasswordAsync(new PasswordChangeRequest
        {
            CurrentPassword = new Password("WrongPassword123!"),
            NewPassword = new Password("NewPassword123!")
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(PasswordChangeRequest.CurrentPassword),
            "Current password is incorrect.");
    }
}
