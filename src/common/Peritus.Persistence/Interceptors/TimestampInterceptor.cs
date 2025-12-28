using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Peritus.Persistence.Entities.Abstractions;

namespace Peritus.Persistence.Interceptors;

public class TimestampInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        UpdateTimestamps(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, ct);
    }

    private void UpdateTimestamps(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<ICreatedEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<IUpdatedEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }
        }
    }
}
