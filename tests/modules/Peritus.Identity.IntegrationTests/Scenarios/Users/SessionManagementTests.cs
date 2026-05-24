using System.Net;
using Microsoft.EntityFrameworkCore;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Users;

public class SessionManagementTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListUserSessions_ReturnsActiveSessionsAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        await SignInAsync(credentials);
        await SignInAsync(credentials);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        // Act
        var response = await api.GetUserSessionsAsync(_ct);

        // Assert
        // CreateUserAsync (via SignUpAsync) creates 1 session, then 3 more SignInAsync calls
        var sessions = ApiAssert.Success(response);
        Assert.Equal(4, sessions.Count);
    }

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
