using Microsoft.EntityFrameworkCore;
using Peritus.Identity.Persistence;
using Peritus.Identity.RestContracts;
using Peritus.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Tokens;

namespace Peritus.Identity.IntegrationTests.Abstractions;

public class IdentityApiTest(InfrastructureFixture fixture) : ApiTest(fixture)
{
    private readonly InfrastructureFixture _fixture = fixture;

    protected IdentityDbContext CreateIdentityDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(_fixture.DataSource!,
            optionsBuilder => optionsBuilder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        options.UseSnakeCaseNamingConvention();

        return new IdentityDbContext(options.Options);
    }

    protected IIdentityApi CreateIdentityApi(AccessToken? accessToken = null)
    {
        return CreateRestClient<IIdentityApi>(accessToken);
    }
}
