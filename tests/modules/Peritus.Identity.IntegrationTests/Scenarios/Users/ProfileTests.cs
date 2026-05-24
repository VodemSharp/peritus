using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Profile;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.IntegrationTests.Types;

namespace Peritus.Identity.IntegrationTests.Scenarios.Users;

public class ProfileTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsEmailAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        // Act
        var response = await api.GetCurrentUserAsync(_ct);

        // Assert
        var user = ApiAssert.Success(response);
        Assert.Equal(credentials.Email.Value, user.Email);
    }

    [Fact]
    public async Task UpdateProfile_WithValidCulture_UpdatesUserAsync()
    {
        // Arrange
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        // Act
        var response = await api.UpdateProfileAsync(new UpdateProfileRequest
        {
            Culture = "uk-UA"
        }, _ct);

        // Assert
        Assert.True(response.IsSuccessful);

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        Assert.Equal("uk-UA", user.Culture);
    }

}
