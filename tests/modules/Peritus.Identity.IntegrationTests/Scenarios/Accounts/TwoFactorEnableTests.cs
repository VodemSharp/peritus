using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Accounts;

public class TwoFactorEnableTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task EnableTwoFactor_ReturnsSecretAndQrUriAsync()
    {
        // Arrange
        var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);

        // Act
        var response = await api.EnableTwoFactorAsync(_ct);

        // Assert
        var result = ApiAssert.Success(response);
        Assert.False(string.IsNullOrWhiteSpace(result.Secret));
        Assert.False(string.IsNullOrWhiteSpace(result.QrCodeUri));
        Assert.Contains(result.Secret, result.QrCodeUri);
    }
}
