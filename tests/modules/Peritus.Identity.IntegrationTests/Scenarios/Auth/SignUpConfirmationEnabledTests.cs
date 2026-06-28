using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.IntegrationTests.Types;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class SignUpConfirmationEnabledTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    protected override Dictionary<string, string?> GetSettings()
    {
        return new Dictionary<string, string?>
        {
            ["IdentityOptions:RequireConfirmedEmail"] = "true"
        };
    }

    [Fact]
    public async Task SignUp_ReturnsNoTokensAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = UserCredentials.Create();
        var request = new SignUpRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        };

        // Act
        var response = await api.SignUpAsync(request, _ct);

        // Assert
        var result = ApiAssert.Success(response);
        Assert.True(result.EmailConfirmationRequired);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
    }
}
