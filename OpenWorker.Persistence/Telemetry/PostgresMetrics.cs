using System.Diagnostics.Metrics;

namespace OpenWorker.Hotspot.Telemetry;

/// <summary>
/// How long the commands sent to Postgres take, tagged with the kind of the SQL statement: <c>SELECT</c>, <c>INSERT</c>,
/// <c>UPDATE</c>, <c>DELETE</c> or <c>OTHER</c> (the migrations, a lock, anything EF Core did not write as a query or a save).<br/>
/// The kind, not the text or the table, is the tag: the text is as many values as there are queries and parameters, and
/// the kind is five. <see cref="PostgresCommandInterceptor"/> feeds it from EF Core, which already times each command.
/// </summary>
public sealed class PostgresMetrics
{
    private readonly Histogram<double> _duration;

    public PostgresMetrics(IMeterFactory factory)
    {
        var meter = OpenWorkerMeter.Create(factory);

        _duration = meter.CreateDuration(
            "postgres.call.duration",
            "How long a command takes to run in Postgres, until its first answer. Tag operation: SELECT, INSERT, UPDATE, DELETE or OTHER.");
    }

    /// <summary>
    /// The value of the <c>operation</c> tag for a SQL statement: its first word, with a query that starts with <c>WITH</c> counted as a <c>SELECT</c>.
    /// A command that carries several statements, as a save does, is named after the first.
    /// </summary>
    public static string Operation(ReadOnlySpan<char> sql)
    {
        sql = sql.TrimStart();

        if (Starts(sql, "SELECT") || Starts(sql, "WITH"))
        {
            return "SELECT";
        }

        if (Starts(sql, "INSERT"))
        {
            return "INSERT";
        }

        if (Starts(sql, "UPDATE"))
        {
            return "UPDATE";
        }

        if (Starts(sql, "DELETE"))
        {
            return "DELETE";
        }

        return "OTHER";
    }

    private static bool Starts(ReadOnlySpan<char> sql, string keyword)
    {
        return sql.StartsWith(keyword, StringComparison.OrdinalIgnoreCase);
    }

    public void Record(ReadOnlySpan<char> sql, TimeSpan duration)
    {
        if (_duration.Enabled)
        {
            _duration.Record(duration.TotalSeconds, new KeyValuePair<string, object?>("operation", Operation(sql)));
        }
    }
}
