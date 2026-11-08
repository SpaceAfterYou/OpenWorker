using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// Reports the commands EF Core runs to <see cref="PostgresMetrics"/>, using the duration EF Core measured around the call.
/// That is the time until the database starts to answer: a result with many rows is read after it, as the caller enumerates it.
/// The commands that fail are measured as well, since a timeout is the slowest call there is.
/// </summary>
public sealed class PostgresCommandInterceptor(PostgresMetrics metrics) : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        metrics.Record(command.CommandText, eventData.Duration);

        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        metrics.Record(command.CommandText, eventData.Duration);

        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        metrics.Record(command.CommandText, eventData.Duration);

        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        metrics.Record(command.CommandText, eventData.Duration);

        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        metrics.Record(command.CommandText, eventData.Duration);

        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        metrics.Record(command.CommandText, eventData.Duration);

        return ValueTask.FromResult(result);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        metrics.Record(command.CommandText, eventData.Duration);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        metrics.Record(command.CommandText, eventData.Duration);

        return Task.CompletedTask;
    }
}
