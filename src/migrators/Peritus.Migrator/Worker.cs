using System.Diagnostics;
using Peritus.Migrator.Migrators;

namespace Peritus.Migrator;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime
) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        const string activityName = "Migrating Database";
        using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client);

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var identityMigrator = scope.ServiceProvider.GetRequiredService<IdentityMigrator>();

            identityMigrator.Migrate();
            await identityMigrator.SeedAsync(ct);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }
}
