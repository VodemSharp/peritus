using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Accounts;

public class TwoFactorGenerateRecoveryCodesTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GenerateRecoveryCodes_With2FaEnabled_ReturnsNewCodesAsync()
    {
        // Arrange
        var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);
        var setup = await SetupTwoFactorAsync(api, _ct);
        var originalCodes = setup.RecoveryCodes;

        // Act
        var response = await api.GenerateRecoveryCodesAsync(_ct);

        // Assert
        var result = ApiAssert.Success(response);
        Assert.Equal(10, result.RecoveryCodes.Length);
        Assert.NotEqual(originalCodes, result.RecoveryCodes);
    }

    [Fact]
    public async Task GenerateRecoveryCodes_Without2FaEnabled_FailsAsync()
    {
        // Arrange
        var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);

        // Act
        var response = await api.GenerateRecoveryCodesAsync(_ct);

        // Assert
        await ApiAssert.ValidationMessageAsync(response, "Two-factor authentication is not enabled.");
    }
}
