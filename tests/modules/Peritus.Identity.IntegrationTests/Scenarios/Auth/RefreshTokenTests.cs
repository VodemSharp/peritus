using System.Net;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Tokens;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class RefreshTokenTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task RefreshToken_WithValidTokens_ReturnsNewTokensAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var initialTokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(initialTokens.AccessToken);

        var request = new RefreshTokenRequest
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
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        await api.SignOutAsync(_ct);

        // Act
        var response = await api.RefreshTokenAsync(new RefreshTokenRequest
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
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        await api.SignOutAsync(_ct);

        TimeProvider.Advance(TimeSpan.FromHours(2));

        // Act
        var response = await api.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(RefreshTokenRequest.RefreshToken),
            "User session has been terminated.");
    }

    [Fact]
    public async Task RefreshToken_AfterRefreshTokenExpiration_ReturnsValidationErrorAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        TimeProvider.Advance(TimeSpan.FromDays(2));

        // Act
        var response = await api.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(RefreshTokenRequest.RefreshToken),
            "User session has been expired.");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsNotFoundAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var request = new RefreshTokenRequest
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = new RefreshToken("invalid")
        };

        // Act
        var response = await api.RefreshTokenAsync(request, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(RefreshTokenRequest.RefreshToken),
            "Refresh token not found.");
    }

    [Fact]
    public async Task RefreshToken_RevokesOldAccessTokenAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var initialTokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(initialTokens.AccessToken);

        var request = new RefreshTokenRequest
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
        var credentials = await CreateUserAsync(_ct);
        var initialTokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(initialTokens.AccessToken);

        var request = new RefreshTokenRequest
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
