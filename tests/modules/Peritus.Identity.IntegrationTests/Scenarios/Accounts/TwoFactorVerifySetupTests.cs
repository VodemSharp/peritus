using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Accounts;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Helpers;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Accounts;

public class TwoFactorVerifySetupTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task VerifyTwoFactorSetup_WithValidCode_Enables2FaAndReturnsRecoveryCodesAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);

        var enableResponse = await api.EnableTwoFactorAsync(_ct);
        var secret = enableResponse.Content!.Secret;

        var code = TotpTestHelper.Generate(secret);

        // Act
        var verifyResponse = await api.ConfirmTwoFactorSetupAsync(new TwoFactorVerifySetupRequest
        {
            Code = code
        }, _ct);

        // Assert
        var result = ApiAssert.Success(verifyResponse);
        Assert.NotEmpty(result.RecoveryCodes);
        Assert.Equal(10, result.RecoveryCodes.Length);

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        Assert.True(user.TwoFactorEnabled);
    }
}
