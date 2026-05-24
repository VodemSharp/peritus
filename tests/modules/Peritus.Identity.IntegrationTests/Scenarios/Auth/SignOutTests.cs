using System.Net;
using Microsoft.EntityFrameworkCore;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class SignOutTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SignOut_WithValidToken_ReturnsOkAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        // Act
        var response = await api.SignOutAsync(_ct);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SignOut_WithValidToken_TerminatesSessionAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        // Act
        await api.SignOutAsync(_ct);

        // Assert
        await using var db = CreateIdentityDbContext();
        var userSession = await db.UserSessions
            .Where(x => x.User!.Email == credentials.Email && x.RefreshToken == tokens.RefreshToken)
            .SingleAsync(_ct);

        Assert.Equal(UserSessionStatus.Terminated, userSession.Status);
    }

    [Fact]
    public async Task SignOut_WithMultipleSessions_TerminatesOnlyTargetSessionAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var firstTokens = await SignInAsync(credentials);
        var secondTokens = await SignInAsync(credentials);
        var firstApi = CreateIdentityApi(firstTokens.AccessToken);

        // Act
        await firstApi.SignOutAsync(_ct);

        // Assert
        await using var db = CreateIdentityDbContext();
        var userSessions = await db.UserSessions
            .Where(us => us.User!.Email == credentials.Email)
            .ToListAsync(_ct);

        // +1 session was created during CreateUserAsync
        Assert.Equal(3, userSessions.Count);
        Assert.Contains(userSessions, s =>
            s.RefreshToken == firstTokens.RefreshToken && s.Status == UserSessionStatus.Terminated);
        Assert.Contains(userSessions, s =>
            s.RefreshToken == secondTokens.RefreshToken && s.Status == UserSessionStatus.Confirmed);
    }
}
