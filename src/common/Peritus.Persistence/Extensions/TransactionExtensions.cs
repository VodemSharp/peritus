using Microsoft.EntityFrameworkCore;

namespace Peritus.Persistence.Extensions;

public static class TransactionExtensions
{
    extension(DbContext db)
    {
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
        {
            var strategy = db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    var result = await action();
                    await db.Database.CommitTransactionAsync(ct);
                    return result;
                }
                catch
                {
                    await db.Database.RollbackTransactionAsync(ct);
                    throw;
                }
            });
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
        {
            await db.ExecuteInTransactionAsync(async () =>
            {
                await action();
                return true;
            }, ct);
        }
    }
}
