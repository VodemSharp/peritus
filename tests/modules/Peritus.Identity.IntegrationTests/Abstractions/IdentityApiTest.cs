using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Peritus.ApiContracts.Identity;
using Peritus.Cache.Distributed;
using Peritus.Identity.IntegrationTests.Infrastructure;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.IntegrationTests.Abstractions;
using Peritus.IntegrationTests.Fixtures;
using Peritus.Types.Tokens;

namespace Peritus.Identity.IntegrationTests.Abstractions;

public class IdentityApiTest(InfrastructureFixture fixture) : ApiTest(fixture)
{
    private readonly InfrastructureFixture _fixture = fixture;

    protected TokenCaptureState TokenCapture { get; } = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(TokenCapture);
        services.AddScoped<IUserTokenService>(sp =>
        {
            var cache = sp.GetRequiredService<IDistributedCacheService>();
            var timeProvider = sp.GetRequiredService<TimeProvider>();
            return new CapturingUserTokenService(cache, timeProvider, TokenCapture);
        });
    }

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
