using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class LockoutTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    protected override Dictionary<string, string?> GetSettings()
    {
        return new Dictionary<string, string?>
        {
            ["IdentityOptions:MaxFailedAccessAttempts"] = "3",
            ["IdentityOptions:DefaultLockoutTimeSpan"] = "00:05:00"
        };
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
