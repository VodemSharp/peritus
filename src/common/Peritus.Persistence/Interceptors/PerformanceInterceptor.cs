using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Peritus.Persistence.Interceptors;

public partial class PerformanceInterceptor(ILogger<PerformanceInterceptor> logger, TimeSpan? slowQueryThreshold = null)
    : DbCommandInterceptor
{
    private readonly TimeSpan _slowQueryThreshold = slowQueryThreshold ?? TimeSpan.FromSeconds(1);

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogWarningIfSlow(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken ct = default)
    {
        LogWarningIfSlow(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, ct);
    }

    private void LogWarningIfSlow(DbCommand command, CommandExecutedEventData eventData)
    {
        var duration = eventData.Duration;

        if (duration > _slowQueryThreshold)
        {
            LogSlowQuery(duration.TotalMilliseconds, command.CommandText);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Slow query detected: {Duration} ms. Command: {CommandText}")]
    private partial void LogSlowQuery(double duration, string commandText);
}
