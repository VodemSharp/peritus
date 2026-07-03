using System.Net;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Tokens;

namespace Peritus.Identity.IntegrationTests.Features.Auth;

public class TokenRefreshTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task RefreshToken_WithValidTokens_ReturnsNewTokensAsync()
    {
        // Arrange
        var (_, initialTokens, api) = await CreateAuthenticatedUserAsync(_ct);

        var request = new TokenRefreshRequest
        {
            AccessToken = initialTokens.AccessToken,
            RefreshToken = initialTokens.RefreshToken
        };

        // Act
        var response = await api.RefreshTokenAsync(request, _ct);

        // Assert
        var refreshedTokens = ApiAssert.Success(response);
        JwtAssert.Valid(refreshedTokens.AccessToken);
        Assert.NotEqual(initialTokens.AccessToken, refreshedTokens.AccessToken);
        Assert.NotEqual(initialTokens.RefreshToken, refreshedTokens.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_AfterSignOut_ReturnsUnauthorizedAsync()
    {
        // Arrange
        var (_, tokens, api) = await CreateAuthenticatedUserAsync(_ct);

        await api.SignOutAsync(_ct);

        // Act
        var response = await api.RefreshTokenAsync(new TokenRefreshRequest
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken
        }, _ct);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_AfterSignOutAndAccessTokenExpiration_ReturnsValidationErrorAsync()
    {
        // Arrange
        var (_, tokens, api) = await CreateAuthenticatedUserAsync(_ct);

        await api.SignOutAsync(_ct);

        TimeProvider.Advance(TimeSpan.FromHours(2));

        // Act
        var response = await api.RefreshTokenAsync(new TokenRefreshRequest
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(TokenRefreshRequest.RefreshToken),
            "User session has been terminated.");
    }

    [Fact]
    public async Task RefreshToken_AfterRefreshTokenExpiration_ReturnsValidationErrorAsync()
    {
        // Arrange
        var (_, tokens, api) = await CreateAuthenticatedUserAsync(_ct);

        TimeProvider.Advance(TimeSpan.FromDays(2));

        // Act
        var response = await api.RefreshTokenAsync(new TokenRefreshRequest
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(TokenRefreshRequest.RefreshToken),
            "User session has been expired.");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsNotFoundAsync()
    {
        // Arrange
        var (_, tokens, api) = await CreateAuthenticatedUserAsync(_ct);

        var request = new TokenRefreshRequest
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = new RefreshToken("invalid")
        };

        // Act
        var response = await api.RefreshTokenAsync(request, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(TokenRefreshRequest.RefreshToken),
            "Refresh token not found.");
    }

    [Fact]
    public async Task RefreshToken_RevokesOldAccessTokenAsync()
    {
        // Arrange
        var (_, initialTokens, api) = await CreateAuthenticatedUserAsync(_ct);

        var request = new TokenRefreshRequest
        {
            AccessToken = initialTokens.AccessToken,
            RefreshToken = initialTokens.RefreshToken
        };

        await api.RefreshTokenAsync(request, _ct);

        // Act
        var response = await api.GetCurrentUserAsync(_ct);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_PreventsReuseAsync()
    {
        // Arrange
        var (_, initialTokens, api) = await CreateAuthenticatedUserAsync(_ct);

        var request = new TokenRefreshRequest
        {
            AccessToken = initialTokens.AccessToken,
            RefreshToken = initialTokens.RefreshToken
        };

        await api.RefreshTokenAsync(request, _ct);

        // Act
        var response = await api.RefreshTokenAsync(request, _ct);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
