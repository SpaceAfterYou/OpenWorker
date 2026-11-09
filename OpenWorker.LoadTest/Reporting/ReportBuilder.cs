using System.Globalization;
using System.Runtime.InteropServices;
using OpenWorker.LoadTest.Bots;
using OpenWorker.LoadTest.Cli;
using OpenWorker.LoadTest.Monitoring;
using OpenWorker.LoadTest.Stats;

namespace OpenWorker.LoadTest.Reporting;

/// <summary>
/// What the report is made of, gathered by the run.
/// </summary>
internal sealed record ReportInputs(
    RunOptions Options,
    RunStats Stats,
    DateTimeOffset StartedAt,
    double TotalSeconds,
    bool Interrupted,
    IReadOnlyList<SelfSample> Self,
    IReadOnlyList<ServerSampler> Servers,
    PrometheusReport? Prometheus,
    IReadOnlyList<TimelinePoint> Timeline,
    IReadOnlyList<string> Notes);

/// <summary>
/// Puts the statistics of a run together into a <see cref="RunReport" />.
/// </summary>
internal static class ReportBuilder
{
    /// <summary>
    /// Above this share of the bot's cores in use the bot is taken to be close to its own limit.
    /// </summary>
    private const double BusyShare = 0.8;

    private const double UnmatchedShare = 0.05;

    public static RunReport Build(ReportInputs input)
    {
        var options = input.Options;
        var stats = input.Stats;
        var bots = stats.Bots();

        var latency = new Dictionary<string, LatencySummary>
        {
            [LatencyNames.MoveEcho] = Summarize(bots, e => e.MoveEcho),
            [LatencyNames.MoveDelivery] = Summarize(bots, e => e.MoveDelivery),
            [LatencyNames.Ping] = Summarize(bots, e => e.Ping),
            [LatencyNames.ChatEcho] = Summarize(bots, e => e.ChatEcho),
            [LatencyNames.ChatDelivery] = Summarize(bots, e => e.ChatDelivery),
            [LatencyNames.MazeMoveEcho] = Summarize(bots, e => e.MazeMoveEcho),
            [LatencyNames.SkillResponse] = Summarize(bots, e => e.SkillResponse),
            [LatencyNames.SendLag] = Summarize(bots, e => e.SendLag)
        };

        var traffic = BuildTraffic(stats, bots, latency);
        var self = BuildSelf(input.Self);
        var servers = BuildServers(input.Servers);
        var notes = new List<string>(input.Notes);

        foreach (var sampler in input.Servers)
        {
            notes.AddRange(sampler.Notes);
        }

        var report = new RunReport(
            "OpenWorker.LoadTest",
            input.StartedAt,
            input.TotalSeconds,
            stats.MeasuredSeconds,
            input.Interrupted,
            new ReportMachine(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, Environment.ProcessorCount),
            DescribeOptions(options),
            new ReportBots(
                options.Bots,
                stats.Started,
                stats.Stage(BotStage.DistrictEnter).Succeeded,
                stats.Active,
                stats.PeakActive,
                stats.Failed,
                stats.Lost,
                bots.Length == 0 ? 0 : Enumerable.Range(0, options.Bots).Count(i => RunContext.GoesToMaze(i, options.MazeShare)),
                stats.InMaze),
            Enum.GetValues<BotStage>()
                .Select(stage => new ReportStage(BotStages.NameOf(stage), stats.Stage(stage).Succeeded, stats.Stage(stage).Failed, stats.Stage(stage).Duration()))
                .ToArray(),
            latency,
            traffic,
            stats.Errors
                .OrderByDescending(e => e.Value.Count)
                .ThenBy(e => e.Key, StringComparer.Ordinal)
                .Select(e => new ReportError(e.Key, e.Value.Count, e.Value.Example))
                .ToArray(),
            self,
            servers,
            input.Prometheus,
            [],
            notes,
            input.Timeline);

        return report with { Warnings = Warn(report, options) };
    }

    private static LatencySummary Summarize(BotMetrics[] bots, Func<BotMetrics, LatencyHistogram> select)
    {
        var total = new LatencyHistogram();

        foreach (var bot in bots)
        {
            total.Add(select(bot));
        }

        return LatencySummary.Of(total);
    }

    private static ReportTraffic BuildTraffic(RunStats stats, BotMetrics[] bots, Dictionary<string, LatencySummary> latency)
    {
        long moves = 0, pings = 0, chats = 0, skills = 0, answered = 0, frames = 0, measuredMoves = 0, unmatched = 0;

        foreach (var bot in bots)
        {
            moves += bot.MovesSent;
            pings += bot.PingsSent;
            chats += bot.ChatsSent;
            skills += bot.SkillsSent;
            answered += bot.SkillsAnswered;
            frames += bot.FramesReceived;
            measuredMoves += bot.MovesSentWhileMeasured;
            unmatched += bot.Unmatched;
        }

        var seconds = stats.MeasuredSeconds;
        var echoes = latency[LatencyNames.MoveEcho].Count;
        var deliveries = latency[LatencyNames.MoveDelivery].Count;

        return new ReportTraffic(
            moves,
            pings,
            chats,
            skills,
            answered,
            frames,
            measuredMoves,
            seconds > 0 ? measuredMoves / seconds : 0,
            seconds > 0 ? deliveries / seconds : 0,

            // Every move is heard by the bots that see it, the mover included.
            measuredMoves > 0 ? (double)(echoes + deliveries) / measuredMoves : 0,
            unmatched);
    }

    private static ReportSelf BuildSelf(IReadOnlyList<SelfSample> samples)
    {
        if (samples.Count == 0)
        {
            return new ReportSelf(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var last = samples[^1];

        return new ReportSelf(
            samples.Average(e => e.CpuCores),
            samples.Max(e => e.CpuCores),
            samples.Max(e => e.WorkingSetBytes) / 1048576d,
            last.GcGen0,
            last.GcGen1,
            last.GcGen2,
            last.GcPauseMilliseconds,
            samples.Max(e => e.Threads),
            samples.Max(e => e.PendingWork));
    }

    private static ReportServer[] BuildServers(IReadOnlyList<ServerSampler> samplers)
    {
        var servers = new List<ReportServer>();

        foreach (var sampler in samplers)
        {
            foreach (var group in sampler.Samples.GroupBy(e => e.Name).OrderBy(e => e.Key, StringComparer.Ordinal))
            {
                servers.Add(new ReportServer(
                    group.Key,
                    sampler.Source,
                    group.Count(),
                    group.Average(e => e.CpuPercent),
                    group.Max(e => e.CpuPercent),
                    group.Average(e => e.MemoryBytes) / 1048576d,
                    group.Max(e => e.MemoryBytes) / 1048576d));
            }
        }

        return [.. servers];
    }

    /// <summary>
    /// The settings of the run that make its figures mean something, in the words of the command line.
    /// </summary>
    internal static Dictionary<string, string> DescribeOptions(RunOptions o)
    {
        string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        var map = new Dictionary<string, string>
        {
            ["bots"] = o.Bots.ToString(CultureInfo.InvariantCulture),
            ["ramp-up"] = Number(o.RampUp) + " per second",
            ["duration"] = Durations.Format(o.Duration),
            ["warmup"] = Durations.Format(o.Warmup),
            ["move-rate"] = Number(o.MoveRate) + " per second",
            ["chat-rate"] = Number(o.ChatRate) + " per second",
            ["maze-share"] = Number(o.MazeShare),
            ["maze-location"] = o.MazeLocation.ToString(CultureInfo.InvariantCulture),
            ["skill-rate"] = Number(o.SkillRate) + " per second",
            ["probe-every"] = o.ProbeEvery.ToString(CultureInfo.InvariantCulture),
            ["auth"] = $"{o.AuthHost}:{o.AuthPort.ToString(CultureInfo.InvariantCulture)}",
            ["accounts"] = $"{AccountNames.Format(o.AccountPrefix, o.AccountOffset)} ..."
        };

        if (o.RedirectHost is not null)
        {
            map["redirect-host"] = o.RedirectHost;
        }

        if (o.Channel > 0)
        {
            map["channel"] = o.Channel.ToString(CultureInfo.InvariantCulture);
        }

        if (o.ChannelFill > 0)
        {
            map["channel-fill"] = o.ChannelFill.ToString(CultureInfo.InvariantCulture);
        }

        if (o.District > 0)
        {
            map["district"] = o.District.ToString(CultureInfo.InvariantCulture);
        }

        return map;
    }

    private static List<string> Warn(RunReport report, RunOptions options)
    {
        var warnings = new List<string>();
        var cores = report.Machine.Cores;

        if (report.Bots.ReachedTheWorld == 0)
        {
            warnings.Add("No bot got into a district, so nothing was measured. The errors below say where they were stopped.");
        }

        else if (report.MeasuredSeconds <= 0)
        {
            warnings.Add("The run ended before the measurement began, so there are no latencies.");
        }

        else if (report.Interrupted)
        {
            warnings.Add($"The run was stopped after {report.MeasuredSeconds.ToString("0.#", CultureInfo.InvariantCulture)} s of measuring, short of the {Durations.Format(options.Duration)} asked for.");
        }

        if (report.Bots.FailedToEnter > 0)
        {
            warnings.Add($"{report.Bots.FailedToEnter.ToString(CultureInfo.InvariantCulture)} of {report.Bots.Started.ToString(CultureInfo.InvariantCulture)} bots never got into the world.");
        }

        if (report.Bots.Lost > 0)
        {
            warnings.Add($"{report.Bots.Lost.ToString(CultureInfo.InvariantCulture)} bots were disconnected by the server after they had got in; the figures describe the ones that stayed.");
        }

        if (report.Self.CpuPeakCores >= cores * BusyShare && cores > 0)
        {
            warnings.Add($"The bot used {report.Self.CpuPeakCores.ToString("0.0", CultureInfo.InvariantCulture)} of the {cores.ToString(CultureInfo.InvariantCulture)} cores of this machine at its peak. The servers share the machine with it, and latencies measured here include its load: run the bot on another machine, or with fewer bots.");
        }

        var lag = report.Latency[LatencyNames.SendLag];
        var interval = options.MoveRate > 0 ? 1000d / options.MoveRate : 0;

        if (lag.Count > 0 && lag.P99Ms > Math.Max(50, interval / 2))
        {
            warnings.Add($"The bot sent its moves late: p99 {lag.P99Ms.ToString("0.0", CultureInfo.InvariantCulture)} ms behind schedule. The bot is a bottleneck, and the latencies include its lag.");
        }

        var heard = report.Latency[LatencyNames.MoveEcho].Count + report.Latency[LatencyNames.MoveDelivery].Count;

        if (report.Traffic.Unmatched > 0 && report.Traffic.Unmatched > (heard + report.Traffic.Unmatched) * UnmatchedShare)
        {
            warnings.Add($"{report.Traffic.Unmatched.ToString(CultureInfo.InvariantCulture)} broadcasts could not be matched to a move that a bot sent, more than {(UnmatchedShare * 100).ToString("0", CultureInfo.InvariantCulture)}% of what was heard. The latencies leave them out.");
        }

        return warnings;
    }
}
