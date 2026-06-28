using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Peritus.ApiContracts.Identity;
using Peritus.ApiContracts.Identity.Accounts;
using Peritus.Cache.Distributed;
using Peritus.Identity.IntegrationTests.Helpers;
using Peritus.Identity.IntegrationTests.Infrastructure;
using Peritus.Identity.IntegrationTests.Types;
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

    protected async Task<AuthenticatedUser> CreateAuthenticatedUserAsync(CancellationToken ct = default)
    {
        var credentials = await CreateUserAsync(ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        return new AuthenticatedUser(credentials, tokens, api);
    }

    protected static async Task<TwoFactorSetup> SetupTwoFactorAsync(IIdentityApi api, CancellationToken ct = default)
    {
        var enableResponse = await api.EnableTwoFactorAsync(ct);
        var secret = enableResponse.Content!.Secret;

        var confirmResponse = await api.ConfirmTwoFactorSetupAsync(new TwoFactorVerifySetupRequest
        {
            Code = TotpTestHelper.Generate(secret)
        }, ct);

        return new TwoFactorSetup(secret, confirmResponse.Content!.RecoveryCodes);
    }
}
