using Microsoft.EntityFrameworkCore;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Accounts;

public class SessionRevokeAllTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task RevokeAllSessions_TerminatesAllExceptCurrentAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var firstTokens = await SignInAsync(credentials);
        await SignInAsync(credentials);
        var api = CreateIdentityApi(firstTokens.AccessToken);

        // Act
        await api.RevokeAllSessionsAsync(_ct);

        // Assert
        await using var db = CreateIdentityDbContext();
        var sessions = await db.UserSessions
            .Where(s => s.User!.Email == credentials.Email)
            .ToListAsync(_ct);

        // CreateUserAsync creates 1 session (signup), then 2 SignInAsync calls = 3 total
        var currentSession = sessions.Single(s => s.RefreshToken == firstTokens.RefreshToken);
        Assert.Equal(UserSessionStatus.Confirmed, currentSession.Status);

        foreach (var session in sessions.Where(s => s.RefreshToken != firstTokens.RefreshToken))
        {
            Assert.Equal(UserSessionStatus.Terminated, session.Status);
        }
    }
}
