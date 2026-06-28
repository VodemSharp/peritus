using Microsoft.EntityFrameworkCore;
using Peritus.ApiContracts.Identity.Profile;
using Peritus.Identity.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Assertions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Identity.IntegrationTests.Scenarios.Profile;

public class ProfileUpdateTests(InfrastructureFixture fixture) : IdentityApiTest(fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task UpdateProfile_WithValidCulture_UpdatesUserAsync()
    {
        // Arrange
        var (credentials, _, api) = await CreateAuthenticatedUserAsync(_ct);

        // Act
        var response = await api.UpdateProfileAsync(new ProfileUpdateRequest
        {
            Culture = "uk-UA"
        }, _ct);

        // Assert
        ApiAssert.Success(response);

        await using var db = CreateIdentityDbContext();
        var user = await db.Users
            .Where(u => u.Email == credentials.Email)
            .SingleAsync(_ct);

        Assert.Equal("uk-UA", user.Culture);
    }
}
