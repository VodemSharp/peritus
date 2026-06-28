using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.IntegrationTests.Assertions;
using Peritus.Identity.Types;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Attributes;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

[Settings("IdentityOptions:MaxFailedAccessAttempts", "3")]
[Settings("IdentityOptions:DefaultLockoutTimeSpan", "00:05:00")]
public class SignInTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SignIn_WithValidCredentials_ReturnsTokensAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        var request = new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        };

        // Act
        var response = await api.SignInAsync(request, _ct);

        // Assert
        var tokens = ApiAssert.Success(response);
        JwtAssert.Valid(tokens.AccessToken!.Value);
        Assert.NotEmpty(tokens.RefreshToken!.Value.Value);
    }

    [Fact]
    public async Task SignIn_WithInvalidCredentials_FailsAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        var request = new SignInRequest
        {
            Email = credentials.Email,
            Password = new Password("WrongPassword123!")
        };

        // Act
        var response = await api.SignInAsync(request, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(SignInRequest.Password), "Invalid credentials.");
    }

    [Fact]
    public async Task SignIn_WithValidCredentials_CreatesSessionAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        var request = new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        };

        // Act
        var response = await api.SignInAsync(request, _ct);

        // Assert
        var tokens = ApiAssert.Success(response);
        await using var db = CreateIdentityDbContext();
        var userSession = await db.UserSessions
            .Include(x => x.User)
            .Where(x => x.RefreshToken == tokens.RefreshToken)
            .SingleAsync(_ct);

        Assert.Equal(credentials.Email, userSession.User!.Email);
        Assert.Equal(UserSessionStatus.Confirmed, userSession.Status);
        Assert.True(userSession.CreatedAt <= UtcNow);
    }

    [Fact]
    public async Task SignIn_WithInvalidCredentials_DoesNotCreateSessionAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        var request = new SignInRequest
        {
            Email = credentials.Email,
            Password = new Password("WrongPassword123!")
        };

        // Act
        await api.SignInAsync(request, _ct);

        // Assert
        await using var db = CreateIdentityDbContext();
        var countUserSessions = await db.UserSessions
            .Where(x => x.User!.Email == credentials.Email)
            .CountAsync(_ct);

        // Only 1 session was created during CreateUserAsync
        Assert.Equal(1, countUserSessions);
    }

    [Fact]
    public async Task MultipleFailedSignIns_LockAccountAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        for (var i = 0; i < 3; i++)
        {
            await api.SignInAsync(new SignInRequest
            {
                Email = credentials.Email,
                Password = new Password("WrongPassword123!")
            }, _ct);
        }

        // Act
        var response = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(SignInRequest.Password),
            "Account locked due to multiple failed attempts. Try again in 5 minutes.");
    }

    [Fact]
    public async Task SuccessfulSignIn_ResetsFailedCountAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        for (var i = 0; i < 2; i++)
        {
            await api.SignInAsync(new SignInRequest
            {
                Email = credentials.Email,
                Password = new Password("WrongPassword123!")
            }, _ct);
        }

        // Successful sign-in resets the failed count
        await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        // Two more failures should not lock the account (count was reset)
        for (var i = 0; i < 2; i++)
        {
            await api.SignInAsync(new SignInRequest
            {
                Email = credentials.Email,
                Password = new Password("WrongPassword123!")
            }, _ct);
        }

        // Act
        var response = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        // Assert
        ApiAssert.Success(response);
    }

    [Fact]
    public async Task AfterLockoutDuration_AccountIsUnlockedAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = await CreateUserAsync(_ct);

        for (var i = 0; i < 3; i++)
        {
            await api.SignInAsync(new SignInRequest
            {
                Email = credentials.Email,
                Password = new Password("WrongPassword123!")
            }, _ct);
        }

        TimeProvider.Advance(TimeSpan.FromMinutes(5));

        // Act
        var response = await api.SignInAsync(new SignInRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        }, _ct);

        // Assert
        ApiAssert.Success(response);
    }
}
