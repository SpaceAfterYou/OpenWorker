using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace OpenWorker.LoadTest.Monitoring;

/// <summary>
/// Samples the containers of the servers with <c>docker stats --no-stream</c>. The command takes a second or two,
/// because Docker measures the CPU over a second, so one call is made per interval and the interval is at least that.
/// </summary>
internal sealed class DockerStatsSampler(IReadOnlyList<string> names, string prefix) : ServerSampler
{
    private const int MaxFailuresInARow = 3;

    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(30);

    public override string Source => "docker stats";

    public override async Task RunAsync(TimeSpan interval, long startTimestamp, CancellationToken cancellation)
    {
        var failures = 0;

        using var timer = new PeriodicTimer(interval);

        try
        {
            do
            {
                var (output, problem) = await RunDockerAsync(cancellation);

                if (output is null)
                {
                    Note(problem!);

                    // Docker is not there, or does not answer: asking again every few seconds will not change it.
                    if (problem!.Contains("not found", StringComparison.Ordinal) || ++failures >= MaxFailuresInARow)
                    {
                        Note("Sampling of the containers stopped.");

                        return;
                    }

                    continue;
                }

                failures = 0;

                var seconds = Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds;
                var matched = 0;

                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!TryParseLine(line, out var name, out var cpu, out var memory))
                    {
                        continue;
                    }

                    if (names.Count == 0 ? !name.StartsWith(prefix, StringComparison.Ordinal) : !names.Contains(name))
                    {
                        continue;
                    }

                    matched++;

                    Add(new ServerSample(name, seconds, cpu, memory));
                }

                if (matched == 0)
                {
                    Note(names.Count == 0
                        ? $"No running container has a name that starts with '{prefix}'."
                        : "None of the named containers is running: " + string.Join(", ", names) + ".");
                }
            }
            while (await timer.WaitForNextTickAsync(cancellation));
        }

        catch (OperationCanceledException)
        {
            // The run is over.
        }
    }

    /// <summary>
    /// Reads one line of <c>docker stats --format '{{json .}}'</c>, e.g.
    /// <c>{"Name":"OpenWorker-Redis","CPUPerc":"0.45%","MemUsage":"12.5MiB / 15.5GiB"}</c>.
    /// </summary>
    internal static bool TryParseLine(string line, out string name, out double cpuPercent, out long memoryBytes)
    {
        name = string.Empty;
        cpuPercent = 0;
        memoryBytes = 0;

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!root.TryGetProperty("Name", out var nameElement) ||
                !root.TryGetProperty("CPUPerc", out var cpuElement) ||
                !root.TryGetProperty("MemUsage", out var memoryElement))
            {
                return false;
            }

            name = nameElement.GetString() ?? string.Empty;

            var cpu = cpuElement.GetString()?.TrimEnd('%');
            var usage = memoryElement.GetString()?.Split('/')[0].Trim();

            // A container that has just started or is stopping reports "--".
            return name.Length > 0 &&
                   double.TryParse(cpu, NumberStyles.Float, CultureInfo.InvariantCulture, out cpuPercent) &&
                   TryParseSize(usage, out memoryBytes);
        }

        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// A size as Docker writes it: <c>12.5MiB</c>, <c>1.2GiB</c>, <c>340kB</c>, <c>0B</c>.
    /// </summary>
    internal static bool TryParseSize(string? text, out long bytes)
    {
        bytes = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var span = text.AsSpan().Trim();
        var split = 0;

        while (split < span.Length && (char.IsAsciiDigit(span[split]) || span[split] == '.'))
        {
            split++;
        }

        if (split == 0 || !double.TryParse(span[..split], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        double factor;

        switch (span[split..].ToString())
        {
            case "B": factor = 1; break;
            case "kB": case "KB": factor = 1e3; break;
            case "MB": factor = 1e6; break;
            case "GB": factor = 1e9; break;
            case "TB": factor = 1e12; break;
            case "KiB": factor = 1024; break;
            case "MiB": factor = 1024d * 1024; break;
            case "GiB": factor = 1024d * 1024 * 1024; break;
            case "TiB": factor = 1024d * 1024 * 1024 * 1024; break;
            default: return false;
        }

        bytes = (long)(number * factor);

        return true;
    }

    private async Task<(string? Output, string? Problem)> RunDockerAsync(CancellationToken cancellation)
    {
        var start = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in (string[])["stats", "--no-stream", "--format", "{{json .}}"])
        {
            start.ArgumentList.Add(argument);
        }

        foreach (var name in names)
        {
            start.ArgumentList.Add(name);
        }

        Process process;

        try
        {
            process = Process.Start(start) ?? throw new InvalidOperationException("The process did not start.");
        }

        catch (Exception e) when (e is Win32Exception or InvalidOperationException)
        {
            return (null, "The 'docker' command was not found or could not start: " + e.Message);
        }

        using (process)
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

            limit.CancelAfter(CallTimeout);

            try
            {
                var output = process.StandardOutput.ReadToEndAsync(limit.Token);
                var error = process.StandardError.ReadToEndAsync(limit.Token);

                await process.WaitForExitAsync(limit.Token);

                var text = await output;
                var problem = (await error).Trim();

                return process.ExitCode == 0
                    ? (text, null)
                    : (null, $"'docker stats' failed with exit code {process.ExitCode.ToString(CultureInfo.InvariantCulture)}: {FirstLine(problem)}");
            }

            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
            {
                Kill(process);

                return (null, $"'docker stats' did not finish within {CallTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)} s.");
            }

            catch (OperationCanceledException)
            {
                Kill(process);

                throw;
            }
        }
    }

    private static string FirstLine(string text)
    {
        var end = text.IndexOf('\n', StringComparison.Ordinal);

        return end < 0 ? text : text[..end];
    }

    private static void Kill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }

        catch (Exception e) when (e is InvalidOperationException or Win32Exception)
        {
            // It ended on its own.
        }
    }
}
