using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Features.Auth;

public class PasswordResetSendTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SendPasswordReset_CreatesTokenAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        // Act
        await api.SendPasswordResetAsync(new PasswordResetSendRequest
        {
            Email = credentials.Email
        }, _ct);

        // Assert
        Assert.NotNull(TokenCapture.Get(UserTokenType.PasswordReset));
    }
}
