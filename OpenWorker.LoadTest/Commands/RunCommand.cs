using System.Diagnostics;
using System.Globalization;
using OpenWorker.LoadTest.Bots;
using OpenWorker.LoadTest.Cli;
using OpenWorker.LoadTest.Monitoring;
using OpenWorker.LoadTest.Reporting;
using OpenWorker.LoadTest.Stats;

namespace OpenWorker.LoadTest.Commands;

/// <summary>
/// Runs the bots, watches the servers and the bot itself while they play, and writes the report.
/// </summary>
internal static class RunCommand
{
    public static async Task<int> RunAsync(RunOptions options, TextWriter output, TextWriter progress, CancellationToken stopping)
    {
        var directory = options.Output ?? Path.Combine("TestResults", "loadtest", DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));

        var stats = new RunStats();
        var context = new RunContext { Options = options, Stats = stats };
        var fleet = new Fleet(context);

        var startedAt = DateTimeOffset.UtcNow;
        var begin = Stopwatch.GetTimestamp();

        var self = new SelfSampler();
        var servers = CreateServerSamplers(options);

        // The samplers and the progress run until the bots are gone; they have a token of their own, because a
        // stop from outside must still let the report be made from what they saw.
        using var watching = new CancellationTokenSource();

        var watchers = new List<Task> { self.RunAsync(options.SampleInterval, begin, watching.Token) };

        watchers.AddRange(servers.Select(e => e.RunAsync(options.SampleInterval, begin, watching.Token)));

        var timeline = new List<TimelinePoint>();

        watchers.Add(WatchAsync(options, stats, fleet, self, timeline, progress, begin, watching.Token));

        progress.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Starting {options.Bots} bots at {options.RampUp:0.##} per second against {options.AuthHost}:{options.AuthPort}."));

        await fleet.RunAsync(stopping);

        var total = Stopwatch.GetElapsedTime(begin).TotalSeconds;
        var finishedAt = DateTimeOffset.UtcNow;

        await watching.CancelAsync();

        await Task.WhenAll(watchers);

        var prometheus = await AskPrometheusAsync(options, total, finishedAt, progress);

        var report = ReportBuilder.Build(new ReportInputs(
            options,
            stats,
            startedAt,
            total,
            fleet.Interrupted,
            self.Samples,
            servers,
            prometheus,
            timeline,
            []));

        Save(directory, report, progress);

        MarkdownReport.Write(output, report);

        if (report.Bots.ReachedTheWorld == 0)
        {
            return 1;
        }

        return fleet.Interrupted ? 130 : 0;
    }

    private static List<ServerSampler> CreateServerSamplers(RunOptions options)
    {
        var samplers = new List<ServerSampler>();

        if (options.Docker)
        {
            samplers.Add(new DockerStatsSampler(options.DockerNames, options.DockerPrefix));
        }

        if (options.Pids.Count > 0)
        {
            samplers.Add(new ProcSampler(options.Pids));
        }

        return samplers;
    }

    private static async Task<PrometheusReport?> AskPrometheusAsync(RunOptions options, double seconds, DateTimeOffset at, TextWriter progress)
    {
        if (options.Prometheus is null)
        {
            return null;
        }

        progress.WriteLine("Asking Prometheus.");

        using var client = new HttpClient();

        // The last scrape of the servers may not have reached Prometheus yet.
        await Task.Delay(TimeSpan.FromSeconds(2));

        return await PrometheusClient.QueryAsync(
            client,
            options.Prometheus,
            PrometheusClient.BuildQueries(options, TimeSpan.FromSeconds(seconds + 2)),
            at.AddSeconds(2),
            CancellationToken.None);
    }

    private static void Save(string directory, RunReport report, TextWriter progress)
    {
        try
        {
            Directory.CreateDirectory(directory);

            File.WriteAllText(Path.Combine(directory, "report.json"), MarkdownReport.ToJson(report));

            using var markdown = new StringWriter(CultureInfo.InvariantCulture);

            MarkdownReport.Write(markdown, report);

            File.WriteAllText(Path.Combine(directory, "report.md"), markdown.ToString());

            progress.WriteLine("Report written to " + Path.GetFullPath(directory));
        }

        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            progress.WriteLine($"The report could not be written to {directory}: {e.Message}");
        }
    }

    /// <summary>
    /// Takes a point of the timeline every sample interval and writes a line of progress every progress interval.
    /// </summary>
    private static async Task WatchAsync(
        RunOptions options,
        RunStats stats,
        Fleet fleet,
        SelfSampler self,
        List<TimelinePoint> timeline,
        TextWriter progress,
        long begin,
        CancellationToken cancellation)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        var second = 0;

        try
        {
            while (await timer.WaitForNextTickAsync(cancellation))
            {
                second++;

                var point = Snapshot(stats, fleet, self, begin);

                if (second % Math.Max(1, (int)options.SampleInterval.TotalSeconds) == 0)
                {
                    timeline.Add(point);
                }

                if (options.Progress > TimeSpan.Zero && second % Math.Max(1, (int)options.Progress.TotalSeconds) == 0)
                {
                    progress.WriteLine(Describe(point, stats));
                }
            }
        }

        catch (OperationCanceledException)
        {
            // The bots are gone.
        }
    }

    private static TimelinePoint Snapshot(RunStats stats, Fleet fleet, SelfSampler self, long begin)
    {
        var moves = stats.Bots().Sum(e => e.MovesSent);
        var samples = self.Samples;

        return new TimelinePoint(
            Stopwatch.GetElapsedTime(begin).TotalSeconds,
            fleet.Phase.ToString(),
            stats.Active,
            stats.Entering,
            stats.Failed,
            moves,
            samples.Count == 0 ? 0 : samples[^1].CpuCores);
    }

    private static string Describe(TimelinePoint point, RunStats stats)
    {
        var line = string.Create(
            CultureInfo.InvariantCulture,
            $"t={point.Seconds:0}s {point.Phase,-9} bots: {point.Active} in, {point.Entering} entering, {point.Failed} failed, {stats.Lost} lost | moves sent {point.MovesSent} | bot cpu {point.BotCpuCores:0.00} cores");

        if (!stats.Measuring)
        {
            return line;
        }

        var echo = new LatencyHistogram();

        foreach (var bot in stats.Bots())
        {
            echo.Add(bot.MoveEcho);
        }

        return line + string.Create(CultureInfo.InvariantCulture, $" | move echo p99 {echo.Percentile(0.99) / 1000d:0.0} ms");
    }
}
