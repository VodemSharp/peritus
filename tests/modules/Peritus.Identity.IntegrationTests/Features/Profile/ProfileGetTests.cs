using System.Net;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Tokens;

namespace Peritus.Identity.IntegrationTests.Features.Profile;

public class ProfileGetTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsEmailAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);

        // Act
        var response = await api.GetCurrentUserAsync(_ct);

        // Assert
        var user = ApiAssert.Success(response);
        Assert.Equal(credentials.Email, user.Email);
    }

    [Fact]
    public async Task GetCurrentUser_WithTamperedToken_IsRejectedAsync()
    {
        // Arrange
        var handler = new JsonWebTokenHandler();
        var securityKey = new SymmetricSecurityKey("invalid-test-key-000000000000000"u8.ToArray());
        var fakeToken = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "invalid-issuer",
            Audience = "invalid-audience",
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        });

        var api = CreateIdentityApi(new AccessToken(fakeToken));

        // Act
        var response = await api.GetCurrentUserAsync(_ct);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithExpiredToken_IsRejectedAsync()
    {
        // Arrange
        var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);

        TimeProvider.Advance(TimeSpan.FromHours(2));

        // Act
        var response = await api.GetCurrentUserAsync(_ct);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
