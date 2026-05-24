using System.Net;
using Peritus.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Fixtures;

namespace Peritus.Api.IntegrationTests;

public class ApiHealthTest(InfrastructureFixture fixture) : ApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetHealthEndpointReturnsOkAndHealthyAsync()
    {
        // Arrange
        using var httpClient = CreateHttpClient();

        // Act
        using var response = await httpClient.GetAsync("/health", _ct);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(_ct);
        Assert.Equal("Healthy", content.Trim());
    }
}
