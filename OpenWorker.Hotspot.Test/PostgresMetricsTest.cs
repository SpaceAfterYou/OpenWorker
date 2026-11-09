using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class PostgresMetricsTest
{
    /// <summary>
    /// A statement is tagged by the keyword it starts with, in any case and after any white space; a query with a
    /// common table expression is a <c>SELECT</c>, and whatever is not one of the four kinds is <c>OTHER</c>.
    /// </summary>
    [TestMethod]
    public void AStatementIsTaggedWithItsKind()
    {
        (string Sql, string Operation)[] cases =
        [
            ("\n   select 1", "SELECT"),
            ("WITH ranked AS (SELECT 1) SELECT * FROM ranked", "SELECT"),
            ("INSERT INTO \"Persons\" (\"Name\") VALUES (@p0) RETURNING \"Id\"", "INSERT"),
            ("UPDATE \"Persons\" SET \"Name\" = @p0 WHERE \"Id\" = @p1", "UPDATE"),
            ("DELETE FROM \"Persons\" WHERE \"Id\" = @p0", "DELETE"),
            ("SET lock_timeout = '1s'", "OTHER"),
            ("", "OTHER")
        ];

        foreach (var (sql, operation) in cases)
        {
            Assert.AreEqual(operation, PostgresMetrics.Operation(sql), sql);
        }
    }

    [TestMethod]
    public void TheTagDoesNotGrowWithTheStatements()
    {
        // The parameters and the names of the tables must never reach the tag: one value for each kind of statement.
        var tags = Enumerable.Range(0, 200)
            .Select(i => PostgresMetrics.Operation($"SELECT * FROM \"Table{i}\" WHERE \"Id\" = {i}"))
            .Distinct()
            .ToArray();

        Assert.HasCount(1, tags);
        Assert.AreEqual("SELECT", tags[0]);
    }

    private static CommandExecutedEventData Executed(NpgsqlCommand command, TimeSpan duration) => new(
        eventDefinition: null!,
        messageGenerator: null!,
        connection: null!,
        command,
        logCommandText: command.CommandText,
        context: null,
        DbCommandMethod.ExecuteReader,
        commandId: Guid.NewGuid(),
        connectionId: Guid.NewGuid(),
        result: null,
        async: true,
        logParameterValues: false,
        startTime: DateTimeOffset.UtcNow,
        duration,
        CommandSource.LinqQuery);

    [TestMethod]
    public async Task EveryCommandEfCoreRunsIsRecordedUnderItsKind()
    {
        using var metrics = new MetricsHost();
        using var durations = metrics.Listen<double>("postgres.call.duration");

        var interceptor = metrics.Get<PostgresCommandInterceptor>();

        using var select = new NpgsqlCommand("SELECT 1");
        using var insert = new NpgsqlCommand("INSERT INTO \"Persons\" DEFAULT VALUES");

        await interceptor.ReaderExecutedAsync(select, Executed(select, TimeSpan.FromMilliseconds(12)), null!);
        await interceptor.NonQueryExecutedAsync(insert, Executed(insert, TimeSpan.FromMilliseconds(3)), 1);
        await interceptor.ScalarExecutedAsync(select, Executed(select, TimeSpan.FromMilliseconds(5)), 1);

        var selects = durations.Values("operation", "SELECT");
        var inserts = durations.Values("operation", "INSERT");

        Assert.HasCount(2, selects);
        Assert.Contains(0.012, selects);
        Assert.Contains(0.005, selects);
        Assert.HasCount(1, inserts);
        Assert.AreEqual(0.003, inserts[0]);
    }

    [TestMethod]
    public void ACommandThatFailsIsRecordedToo()
    {
        using var metrics = new MetricsHost();
        using var durations = metrics.Listen<double>("postgres.call.duration");

        var interceptor = metrics.Get<PostgresCommandInterceptor>();

        using var update = new NpgsqlCommand("UPDATE \"Persons\" SET \"Name\" = 'x'");

        var failure = new CommandErrorEventData(
            eventDefinition: null!,
            messageGenerator: null!,
            connection: null!,
            update,
            logCommandText: update.CommandText,
            context: null,
            DbCommandMethod.ExecuteNonQuery,
            commandId: Guid.NewGuid(),
            connectionId: Guid.NewGuid(),
            exception: new TimeoutException(),
            async: false,
            logParameterValues: false,
            startTime: DateTimeOffset.UtcNow,
            duration: TimeSpan.FromSeconds(30),
            CommandSource.SaveChanges);

        interceptor.CommandFailed(update, failure);

        var updates = durations.Values("operation", "UPDATE");

        Assert.HasCount(1, updates);
        Assert.AreEqual(30.0, updates[0]);
    }
}
