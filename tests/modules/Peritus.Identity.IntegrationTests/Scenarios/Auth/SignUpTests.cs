using Microsoft.EntityFrameworkCore;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.Identity.RestContracts.Auth;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.IntegrationTests.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.IntegrationTests.Scenarios.Auth;

public class SignUpTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SignUp_WithValidRequest_ReturnsTokensAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = UserCredentials.Create();
        var request = new SignUpRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        };

        // Act
        var response = await api.SignUpAsync(request, _ct);

        // Assert
        ApiAssert.Success(response, tokens =>
        {
            Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        });
    }

    [Fact]
    public async Task SignUp_WithValidRequest_CreatesUserInDatabaseAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = UserCredentials.Create();
        var request = new SignUpRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        };

        // Act
        var response = await api.SignUpAsync(request, _ct);

        // Assert
        await ApiAssert.SuccessAsync(response, async _ =>
        {
            await using var db = CreateIdentityDbContext();
            var user = await db.Users
                .Where(u => u.Email == credentials.Email)
                .SingleOrDefaultAsync(_ct);

            Assert.NotNull(user);
        });
    }

    [Fact]
    public async Task SignUp_WithDuplicateEmail_FailsAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = UserCredentials.Create();
        var request = new SignUpRequest
        {
            Email = credentials.Email,
            Password = credentials.Password
        };

        await api.SignUpAsync(request, _ct);

        // Act
        var response = await api.SignUpAsync(request, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(SignUpRequest.Email),
            "Email address is already in use.");
    }

    [Fact]
    public async Task SignUp_WithEmailCaseInsensitivity_ReturnsDuplicateErrorAsync()
    {
        // Arrange
        var api = CreateIdentityApi();
        var credentials = UserCredentials.Create();
        var lowerCaseEmail = credentials.Email.Value.ToLowerInvariant();
        var upperCaseEmail = credentials.Email.Value.ToUpperInvariant();

        var firstRequest = new SignUpRequest
        {
            Email = new Email(lowerCaseEmail),
            Password = credentials.Password
        };

        await api.SignUpAsync(firstRequest, _ct);

        var secondRequest = new SignUpRequest
        {
            Email = new Email(upperCaseEmail),
            Password = credentials.Password
        };

        // Act
        var response = await api.SignUpAsync(secondRequest, _ct);

        // Assert
        await ApiAssert.ValidationErrorAsync(response, nameof(SignUpRequest.Email),
            "Email address is already in use.");
    }
}
