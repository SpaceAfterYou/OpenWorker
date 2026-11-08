using System.Globalization;
using System.Text.Json;
using OpenWorker.LoadTest.Cli;

namespace OpenWorker.LoadTest.Monitoring;

/// <summary>
/// One series of an answer of Prometheus: the labels that tell which server, and the value.
/// </summary>
internal sealed record PrometheusSeries(IReadOnlyDictionary<string, string> Labels, double Value);

/// <summary>
/// The answer to one query, or why there is none.
/// </summary>
internal sealed record PrometheusResult(string Label, string Query, IReadOnlyList<PrometheusSeries> Series, string? Note);

/// <summary>
/// What the report says about Prometheus: whether it was asked, and the answers.
/// </summary>
internal sealed record PrometheusReport(string? Address, string? Note, IReadOnlyList<PrometheusResult> Results);

/// <summary>
/// Asks Prometheus, after the run, what the servers saw: how long the world loop was in the way, and how long the
/// handlers took. The names of the metrics are the ones the servers export, which Prometheus rewrites when it takes
/// them over OpenTelemetry: dots become underscores, the unit is added (<c>_seconds</c> for the durations, which the servers record in seconds), and a histogram
/// is stored as <c>_bucket</c>, <c>_sum</c> and <c>_count</c>. The names are settings, not guesses in the code.
/// </summary>
internal static class PrometheusClient
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The metrics asked for by default, by the key that <c>--prom-metric</c> renames, with the name Prometheus has
    /// for them: the servers record durations in seconds (unit <c>s</c>), so the figures of the report are in seconds.
    /// </summary>
    public static IReadOnlyDictionary<string, string> DefaultMetrics { get; } = new Dictionary<string, string>
    {
        ["worldloop-queue"] = "worldloop_queue_length",
        ["worldloop-callback"] = "worldloop_callback_duration_seconds",
        ["simulation-step"] = "simulation_step_duration_seconds",
        ["handler"] = "handler_duration_seconds"
    };

    /// <summary>
    /// The queries of the report for a run that lasted <paramref name="window" />.
    /// </summary>
    public static IReadOnlyList<(string Label, string Query)> BuildQueries(RunOptions options, TimeSpan window)
    {
        var range = Math.Max(1, (int)Math.Ceiling(window.TotalSeconds)).ToString(CultureInfo.InvariantCulture) + "s";
        var by = string.IsNullOrWhiteSpace(options.PrometheusGroupBy) ? string.Empty : $" by ({options.PrometheusGroupBy})";
        var byLe = string.IsNullOrWhiteSpace(options.PrometheusGroupBy) ? " by (le)" : $" by ({options.PrometheusGroupBy}, le)";

        string Metric(string key) => options.PrometheusMetrics.TryGetValue(key, out var name) ? name : DefaultMetrics[key];

        var queue = Metric("worldloop-queue");
        var queries = new List<(string, string)>
        {
            ($"{queue} (max)", $"max{by} (max_over_time({queue}[{range}]))"),
            ($"{queue} (mean)", $"avg{by} (avg_over_time({queue}[{range}]))")
        };

        foreach (var key in (string[])["worldloop-callback", "simulation-step", "handler"])
        {
            var metric = Metric(key);

            queries.Add(($"{metric} (p50)", $"histogram_quantile(0.5, sum{byLe} (rate({metric}_bucket[{range}])))"));
            queries.Add(($"{metric} (p99)", $"histogram_quantile(0.99, sum{byLe} (rate({metric}_bucket[{range}])))"));
            queries.Add(($"{metric} (mean)", $"sum{by} (rate({metric}_sum[{range}])) / sum{by} (rate({metric}_count[{range}]))"));
            queries.Add(($"{metric} (per second)", $"sum{by} (rate({metric}_count[{range}]))"));
        }

        foreach (var (label, query) in options.PrometheusQueries)
        {
            queries.Add((label, query.Replace("{window}", range, StringComparison.Ordinal)));
        }

        return queries;
    }

    /// <summary>
    /// Runs the queries as of the end of the run. A Prometheus that cannot be reached skips the whole step with a note.
    /// </summary>
    public static async Task<PrometheusReport> QueryAsync(
        HttpClient client,
        string address,
        IReadOnlyList<(string Label, string Query)> queries,
        DateTimeOffset at,
        CancellationToken cancellation)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var root) || root.Scheme is not ("http" or "https"))
        {
            return new PrometheusReport(address, $"'{address}' is not an http address; the step is skipped.", []);
        }

        var results = new List<PrometheusResult>();

        foreach (var (label, query) in queries)
        {
            // Prometheus takes the time in seconds, with a fraction.
            var uri = new Uri(root, "api/v1/query?query=" + Uri.EscapeDataString(query) + "&time=" + (at.ToUnixTimeMilliseconds() / 1000d).ToString("F3", CultureInfo.InvariantCulture));

            try
            {
                using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

                limit.CancelAfter(RequestTimeout);

                using var response = await client.GetAsync(uri, limit.Token);
                var body = await response.Content.ReadAsStringAsync(limit.Token);

                if (!response.IsSuccessStatusCode)
                {
                    results.Add(new PrometheusResult(label, query, [], $"HTTP {(int)response.StatusCode}: {Shorten(body)}"));

                    continue;
                }

                var series = ParseVector(body, out var problem);

                results.Add(new PrometheusResult(
                    label,
                    query,
                    series,
                    problem ?? (series.Count == 0 ? "no data (the metric is not exported, or has another name: see --prom-metric)" : null)));
            }

            catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !cancellation.IsCancellationRequested)
            {
                return new PrometheusReport(address, $"Prometheus at {address} is not reachable ({e.Message}); the step is skipped.", results);
            }
        }

        return new PrometheusReport(address, null, results);
    }

    /// <summary>
    /// Reads the answer of <c>/api/v1/query</c> for an instant vector:
    /// <c>{"status":"success","data":{"resultType":"vector","result":[{"metric":{"job":"x"},"value":[1700000000.5,"12.5"]}]}}</c>.
    /// A value that is not a number (<c>NaN</c> from a division by zero) is left out.
    /// </summary>
    internal static List<PrometheusSeries> ParseVector(string json, out string? problem)
    {
        problem = null;

        var series = new List<PrometheusSeries>();

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.GetProperty("status").GetString() != "success")
            {
                problem = root.TryGetProperty("error", out var error) ? error.GetString() : "the query failed";

                return series;
            }

            var data = root.GetProperty("data");

            if (data.GetProperty("resultType").GetString() != "vector")
            {
                problem = "the answer is not an instant vector";

                return series;
            }

            foreach (var item in data.GetProperty("result").EnumerateArray())
            {
                var labels = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var label in item.GetProperty("metric").EnumerateObject())
                {
                    labels[label.Name] = label.Value.GetString() ?? string.Empty;
                }

                var text = item.GetProperty("value")[1].GetString();

                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value))
                {
                    series.Add(new PrometheusSeries(labels, value));
                }
            }
        }

        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            problem = "the answer cannot be read: " + e.Message;
        }

        return series;
    }

    private static string Shorten(string text)
    {
        var line = text.ReplaceLineEndings(" ").Trim();

        return line.Length <= 200 ? line : line[..200] + "...";
    }
}
