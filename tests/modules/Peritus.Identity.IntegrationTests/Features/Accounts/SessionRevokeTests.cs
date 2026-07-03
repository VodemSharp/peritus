using System.Net;
using Microsoft.EntityFrameworkCore;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Features.Accounts;

public class SessionRevokeTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task RevokeUserSession_TerminatesSessionAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var firstTokens = await SignInAsync(credentials);
        var secondTokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(firstTokens.AccessToken);

        await using var arrangeDb = CreateIdentityDbContext();
        var secondSession = await arrangeDb.UserSessions
            .Where(s => s.RefreshToken == secondTokens.RefreshToken)
            .SingleAsync(_ct);

        // Act
        var revokeResponse = await api.RevokeUserSessionAsync(secondSession.Id.Value.ToString(), _ct);

        // Assert
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        await using var assertDb = CreateIdentityDbContext();
        var terminatedSession = await assertDb.UserSessions
            .Where(s => s.RefreshToken == secondTokens.RefreshToken)
            .SingleAsync(_ct);

        Assert.Equal(UserSessionStatus.Terminated, terminatedSession.Status);
    }
}
