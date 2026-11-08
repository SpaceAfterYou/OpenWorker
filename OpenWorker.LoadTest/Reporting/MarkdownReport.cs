using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenWorker.LoadTest.Stats;

namespace OpenWorker.LoadTest.Reporting;

/// <summary>
/// Writes a <see cref="RunReport" /> as Markdown for the person who ran it and as JSON for whatever reads it next.
/// Every number is formatted with the invariant culture.
/// </summary>
internal static class MarkdownReport
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToJson(RunReport report)
    {
        return JsonSerializer.Serialize(report, Json);
    }

    public static void Write(TextWriter output, RunReport report)
    {
        output.WriteLine("# OpenWorker load test");
        output.WriteLine();
        output.WriteLine(Inv($"Started {report.StartedAt:yyyy-MM-dd HH:mm:ss}Z, ran {Seconds(report.TotalSeconds)}, measured {Seconds(report.MeasuredSeconds)}{(report.Interrupted ? ", stopped before the end" : string.Empty)}."));
        output.WriteLine();
        output.WriteLine(Inv($"Bot machine: {report.Machine.Os}, {report.Machine.Framework}, {report.Machine.Cores} cores."));

        WriteWarnings(output, report);
        WriteScenario(output, report);
        WriteBots(output, report);
        WriteStages(output, report);
        WriteLatency(output, report);
        WriteTraffic(output, report);
        WriteErrors(output, report);
        WriteSelf(output, report);
        WriteServers(output, report);
        WritePrometheus(output, report);
        WriteNotes(output, report);
    }

    private static void WriteWarnings(TextWriter output, RunReport report)
    {
        if (report.Warnings.Count == 0)
        {
            return;
        }

        Heading(output, "Read this first");

        foreach (var warning in report.Warnings)
        {
            output.WriteLine("- " + warning);
        }
    }

    private static void WriteScenario(TextWriter output, RunReport report)
    {
        Heading(output, "Scenario");

        output.WriteLine("| setting | value |");
        output.WriteLine("|---|---|");

        foreach (var (key, value) in report.Options)
        {
            output.WriteLine($"| {key} | {value} |");
        }
    }

    private static void WriteBots(TextWriter output, RunReport report)
    {
        var bots = report.Bots;

        Heading(output, "Bots");

        output.WriteLine("| | |");
        output.WriteLine("|---|---|");
        output.WriteLine(Inv($"| asked for | {bots.Requested} |"));
        output.WriteLine(Inv($"| started | {bots.Started} |"));
        output.WriteLine(Inv($"| reached a district | {bots.ReachedTheWorld} |"));
        output.WriteLine(Inv($"| in the world at the end | {bots.ActiveAtEnd} (peak {bots.PeakActive}) |"));
        output.WriteLine(Inv($"| failed before they got in | {bots.FailedToEnter} |"));
        output.WriteLine(Inv($"| disconnected after they got in | {bots.Lost} |"));
        output.WriteLine(Inv($"| chosen to fight in a maze | {bots.MazeBots} (in a maze at the end: {bots.InMazeAtEnd}) |"));
    }

    private static void WriteStages(TextWriter output, RunReport report)
    {
        Heading(output, "Steps into the world");

        output.WriteLine("Time is what the step took for the bots that got through it, in ms.");
        output.WriteLine();
        output.WriteLine("| step | passed | failed | p50 | p95 | p99 | max |");
        output.WriteLine("|---|---:|---:|---:|---:|---:|---:|");

        foreach (var stage in report.Stages.Where(e => e.Succeeded + e.Failed > 0))
        {
            output.WriteLine(Inv($"| {stage.Name} | {stage.Succeeded} | {stage.Failed} | {Ms(stage.Duration.P50Ms, stage.Duration.Count)} | {Ms(stage.Duration.P95Ms, stage.Duration.Count)} | {Ms(stage.Duration.P99Ms, stage.Duration.Count)} | {Ms(stage.Duration.MaxMs, stage.Duration.Count)} |"));
        }
    }

    private static void WriteLatency(TextWriter output, RunReport report)
    {
        Heading(output, "Latency in the measured time (ms)");

        output.WriteLine("| what | samples | mean | p50 | p95 | p99 | max |");
        output.WriteLine("|---|---:|---:|---:|---:|---:|---:|");

        var shown = new List<(string Name, string Meaning)>();

        foreach (var (name, meaning) in LatencyNames.All)
        {
            var summary = report.Latency[name];

            // The movement rows are always there, so a missing figure is seen to be missing.
            if (summary.Count == 0 && name is not (LatencyNames.MoveEcho or LatencyNames.MoveDelivery or LatencyNames.Ping))
            {
                continue;
            }

            shown.Add((name, meaning));

            output.WriteLine(Inv($"| {name} | {summary.Count} | {Ms(summary.MeanMs, summary.Count)} | {Ms(summary.P50Ms, summary.Count)} | {Ms(summary.P95Ms, summary.Count)} | {Ms(summary.P99Ms, summary.Count)} | {Ms(summary.MaxMs, summary.Count)} |"));
        }

        output.WriteLine();
        output.WriteLine("Percentiles are read from buckets that are at most 3% wide and are given as the top of the bucket, so a figure is never below the real one; the maximum is exact.");
        output.WriteLine();

        foreach (var (name, meaning) in shown)
        {
            output.WriteLine($"- `{name}`: {meaning}");
        }
    }

    private static void WriteTraffic(TextWriter output, RunReport report)
    {
        var traffic = report.Traffic;

        Heading(output, "Traffic");

        output.WriteLine("| | |");
        output.WriteLine("|---|---|");
        output.WriteLine(Inv($"| moves sent | {traffic.MovesSent} ({traffic.MovesSentWhileMeasured} while measured, {traffic.MovesPerSecond:0.#} per second) |"));
        output.WriteLine(Inv($"| pings, chat lines, skills sent | {traffic.PingsSent}, {traffic.ChatsSent}, {traffic.SkillsSent} (skills answered: {traffic.SkillsAnswered}) |"));
        output.WriteLine(Inv($"| moves delivered to other bots | {traffic.DeliveriesPerSecond:0.#} per second |"));
        output.WriteLine(Inv($"| bots that hear a move, on average | {traffic.AverageListeners:0.#} (the size of the channel the bots were in, the mover included) |"));
        output.WriteLine(Inv($"| frames received | {traffic.FramesReceived} |"));
        output.WriteLine(Inv($"| broadcasts not matched to a move | {traffic.Unmatched} |"));
    }

    private static void WriteErrors(TextWriter output, RunReport report)
    {
        Heading(output, "Errors");

        if (report.Errors.Count == 0)
        {
            output.WriteLine("None.");

            return;
        }

        output.WriteLine("| step: kind | count | example |");
        output.WriteLine("|---|---:|---|");

        foreach (var error in report.Errors.Take(40))
        {
            output.WriteLine(Inv($"| {error.Kind} | {error.Count} | {Line(error.Example)} |"));
        }

        if (report.Errors.Count > 40)
        {
            output.WriteLine();
            output.WriteLine(Inv($"{report.Errors.Count - 40} more kinds are in the JSON file."));
        }
    }

    private static void WriteSelf(TextWriter output, RunReport report)
    {
        var self = report.Self;

        Heading(output, "The bot itself");

        output.WriteLine("| | |");
        output.WriteLine("|---|---|");
        output.WriteLine(Inv($"| cpu | {self.CpuAverageCores:0.00} cores on average, {self.CpuPeakCores:0.00} at the peak, of {report.Machine.Cores} |"));
        output.WriteLine(Inv($"| memory | {self.WorkingSetMaxMegabytes:0} MB at most |"));
        output.WriteLine(Inv($"| garbage collections | {self.GcGen0} / {self.GcGen1} / {self.GcGen2} (gen 0 / 1 / 2), {self.GcPauseMilliseconds:0} ms paused |"));
        output.WriteLine(Inv($"| thread pool | {self.ThreadsMax} threads at most, {self.PendingWorkMax} items waiting at most |"));
    }

    private static void WriteServers(TextWriter output, RunReport report)
    {
        Heading(output, "Servers");

        if (report.Servers.Count == 0)
        {
            output.WriteLine("Not sampled (see --docker-stats and --pids).");

            return;
        }

        output.WriteLine("CPU is in percent of one core.");
        output.WriteLine();
        output.WriteLine("| server | from | samples | cpu avg | cpu max | memory avg (MB) | memory max (MB) |");
        output.WriteLine("|---|---|---:|---:|---:|---:|---:|");

        foreach (var server in report.Servers)
        {
            output.WriteLine(Inv($"| {server.Name} | {server.Source} | {server.Samples} | {server.CpuAveragePercent:0.0} | {server.CpuMaxPercent:0.0} | {server.MemoryAverageMegabytes:0} | {server.MemoryMaxMegabytes:0} |"));
        }
    }

    private static void WritePrometheus(TextWriter output, RunReport report)
    {
        if (report.Prometheus is not { } prometheus)
        {
            return;
        }

        Heading(output, "Loop and handlers (Prometheus)");

        if (prometheus.Note is not null)
        {
            output.WriteLine(prometheus.Note);

            return;
        }

        output.WriteLine($"From {prometheus.Address}, over the length of the run, in the unit of each metric.");
        output.WriteLine();
        output.WriteLine("| query | server | value |");
        output.WriteLine("|---|---|---:|");

        foreach (var result in prometheus.Results)
        {
            if (result.Note is not null)
            {
                output.WriteLine($"| {result.Label} | | {result.Note} |");

                continue;
            }

            foreach (var series in result.Series)
            {
                var who = series.Labels.Count == 0 ? "all" : string.Join(", ", series.Labels.Select(e => e.Value));

                output.WriteLine(Inv($"| {result.Label} | {who} | {series.Value:0.###} |"));
            }
        }
    }

    private static void WriteNotes(TextWriter output, RunReport report)
    {
        if (report.Notes.Count == 0)
        {
            return;
        }

        Heading(output, "Notes");

        foreach (var note in report.Notes)
        {
            output.WriteLine("- " + note);
        }
    }

    private static void Heading(TextWriter output, string title)
    {
        output.WriteLine();
        output.WriteLine("## " + title);
        output.WriteLine();
    }

    private static string Inv(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    private static string Seconds(double value) => Inv($"{value:0.0} s");

    private static string Ms(double value, long samples) => samples == 0 ? "-" : value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Line(string text)
    {
        var line = text.ReplaceLineEndings(" ").Replace("|", "/", StringComparison.Ordinal).Trim();

        return line.Length <= 140 ? line : line[..140] + "...";
    }
}
