using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Features.Accounts;

public class SessionListTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
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
}
