using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Assertions;
using Peritus.Identity.IntegrationTests.Infrastructure;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.IntegrationTests.Features.Auth;

public class SignInGoogleTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly FakeGoogleTokenValidator _googleValidator = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<IGoogleTokenValidator>(_googleValidator);
    }

    [Fact]
    public async Task SignInWithGoogle_NewUser_CreatesUserAndReturnsTokensAsync()
    {
        // Arrange
        var email = $"google-{Guid.NewGuid():N}@example.com";
        var sub = Guid.NewGuid().ToString();
        _googleValidator.Payload = BuildPayload(sub, email);

        var api = CreateIdentityApi();

        // Act
        var response = await api.GoogleSignInAsync(new SignInGoogleRequest
        {
            IdToken = "fake-google-id-token"
        }, _ct);

        // Assert
        var result = ApiAssert.Success(response);
        JwtAssert.Valid(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken.Value);

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == new Email(email))
            .SingleAsync(_ct);

        Assert.True(user.EmailConfirmed);

        var externalLogin = await db.UserExternalLogins
            .Where(x => x.ProviderKey == sub)
            .SingleAsync(_ct);

        Assert.Equal(user.Id, externalLogin.UserId);
    }

    [Fact]
    public async Task SignInWithGoogle_ExistingExternalLogin_ReturnsTokensAsync()
    {
        // Arrange
        var email = $"google-{Guid.NewGuid():N}@example.com";
        var sub = Guid.NewGuid().ToString();
        _googleValidator.Payload = BuildPayload(sub, email);

        var api = CreateIdentityApi();

        var firstResponse = await api.GoogleSignInAsync(new SignInGoogleRequest
        {
            IdToken = "fake-google-id-token"
        }, _ct);
        ApiAssert.Success(firstResponse);

        // Act
        var secondResponse = await api.GoogleSignInAsync(new SignInGoogleRequest
        {
            IdToken = "fake-google-id-token"
        }, _ct);

        // Assert
        var result = ApiAssert.Success(secondResponse);
        JwtAssert.Valid(result.AccessToken);

        await using var db = CreateIdentityDbContext();
        var userCount = await db.Users
            .Where(u => u.Email == new Email(email))
            .CountAsync(_ct);
        Assert.Equal(1, userCount);

        var externalLoginCount = await db.UserExternalLogins
            .Where(x => x.ProviderKey == sub)
            .CountAsync(_ct);
        Assert.Equal(1, externalLoginCount);
    }

    [Fact]
    public async Task SignInWithGoogle_InvalidToken_FailsAsync()
    {
        // Arrange
        _googleValidator.Payload = null;
        var api = CreateIdentityApi();

        // Act
        var response = await api.GoogleSignInAsync(new SignInGoogleRequest
        {
            IdToken = "invalid-token"
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(SignInGoogleRequest.IdToken),
            "Invalid or unverified Google token.");
    }

    [Fact]
    public async Task SignInWithGoogle_UnverifiedEmail_FailsAsync()
    {
        // Arrange
        _googleValidator.Payload = BuildPayload(
            Guid.NewGuid().ToString(),
            $"google-{Guid.NewGuid():N}@example.com",
            false);

        var api = CreateIdentityApi();

        // Act
        var response = await api.GoogleSignInAsync(new SignInGoogleRequest
        {
            IdToken = "fake-google-id-token"
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(SignInGoogleRequest.IdToken),
            "Invalid or unverified Google token.");
    }

    private static GoogleSignInPayload BuildPayload(string sub, string email, bool emailVerified = true)
    {
        return new GoogleSignInPayload
        {
            Sub = sub,
            Email = email,
            EmailVerified = emailVerified,
            Name = "Test User"
        };
    }
}
