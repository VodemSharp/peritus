using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class SignInRecoveryCodeTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task RecoveryCodeSignIn_WithValidCode_ReturnsTokensAndConsumesCodeAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);
        var setup = await SetupTwoFactorAsync(api, _ct);
        var recoveryCode = setup.RecoveryCodes[0];

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        var signInResponse = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        var twoFactorToken = signInResponse.Content!.TwoFactorToken!;

        // Act
        var recoveryCodeSignInResponse = await api.RecoveryCodeSignInAsync(new SignInRecoveryCodeRequest
        {
            TwoFactorToken = twoFactorToken,
            Code = recoveryCode
        }, _ct);

        // Assert
        var result = ApiAssert.Success(recoveryCodeSignInResponse);
        JwtAssert.Valid(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken.Value);

        var redeemedCount = await db.UserRecoveryCodes
            .CountAsync(r => r.UserId == user.Id && r.RedeemedAt != null, _ct);

        Assert.Equal(1, redeemedCount);
    }
}
