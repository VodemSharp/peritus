using System.Net;
using Microsoft.EntityFrameworkCore;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Accounts;

public class TwoFactorDisableTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task DisableTwoFactor_Removes2FaAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);
        await SetupTwoFactorAsync(api, _ct);

        // Act
        var disableResponse = await api.DisableTwoFactorAsync(_ct);

        // Assert
        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.TwoFactorSecret);

        var recoveryCodes = await db.UserRecoveryCodes
            .Where(r => r.UserId == user.Id)
            .CountAsync(_ct);

        Assert.Equal(0, recoveryCodes);
    }
}
