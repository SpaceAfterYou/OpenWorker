using System.Diagnostics;
using System.Globalization;

namespace OpenWorker.LoadTest.Monitoring;

/// <summary>
/// Samples processes by the number the system gives them, from <c>/proc/&lt;pid&gt;</c>: the processor time from
/// <c>stat</c> and the resident memory from <c>status</c>. Linux only; a process that ends is noted and the others
/// go on.
/// </summary>
internal sealed class ProcSampler(IReadOnlyList<(string Name, int Pid)> processes) : ServerSampler
{
    /// <summary>
    /// The unit of the times in <c>/proc/&lt;pid&gt;/stat</c>. It is 100 on every mainstream Linux; the program cannot
    /// ask the system for it without calling into C.
    /// </summary>
    private const double ClockTicksPerSecond = 100;

    public override string Source => "/proc";

    public override async Task RunAsync(TimeSpan interval, long startTimestamp, CancellationToken cancellation)
    {
        if (!OperatingSystem.IsLinux())
        {
            Note("Sampling by process number needs Linux (/proc).");

            return;
        }

        var last = new Dictionary<int, (long Timestamp, long Ticks)>();
        var gone = new HashSet<int>();

        using var timer = new PeriodicTimer(interval);

        try
        {
            do
            {
                var now = Stopwatch.GetTimestamp();

                foreach (var (name, pid) in processes)
                {
                    if (gone.Contains(pid))
                    {
                        continue;
                    }

                    if (!TryRead(pid, out var ticks, out var memory))
                    {
                        gone.Add(pid);

                        Note($"The process {name} ({pid.ToString(CultureInfo.InvariantCulture)}) cannot be read; it has ended or is not visible.");

                        continue;
                    }

                    if (last.TryGetValue(pid, out var before))
                    {
                        var elapsed = Stopwatch.GetElapsedTime(before.Timestamp, now).TotalSeconds;
                        var cpu = elapsed > 0 ? (ticks - before.Ticks) / ClockTicksPerSecond / elapsed * 100d : 0;

                        Add(new ServerSample(name, Stopwatch.GetElapsedTime(startTimestamp, now).TotalSeconds, cpu, memory));
                    }

                    last[pid] = (now, ticks);
                }
            }
            while (await timer.WaitForNextTickAsync(cancellation));
        }

        catch (OperationCanceledException)
        {
            // The run is over.
        }
    }

    private static bool TryRead(int pid, out long ticks, out long memoryBytes)
    {
        ticks = 0;
        memoryBytes = 0;

        try
        {
            var directory = "/proc/" + pid.ToString(CultureInfo.InvariantCulture);

            return TryParseStat(File.ReadAllText(directory + "/stat"), out ticks) &&
                   TryParseResidentMemory(File.ReadAllText(directory + "/status"), out memoryBytes);
        }

        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// The user and system time of a process in clock ticks. The name of the process, in parentheses, may hold
    /// spaces and parentheses itself, so the fields are counted from the last closing one.
    /// </summary>
    internal static bool TryParseStat(string stat, out long ticks)
    {
        ticks = 0;

        var close = stat.LastIndexOf(')');

        if (close < 0)
        {
            return false;
        }

        // After the name: state is field 3, and utime and stime are fields 14 and 15.
        var fields = stat[(close + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length < 13 ||
            !long.TryParse(fields[11], NumberStyles.None, CultureInfo.InvariantCulture, out var user) ||
            !long.TryParse(fields[12], NumberStyles.None, CultureInfo.InvariantCulture, out var system))
        {
            return false;
        }

        ticks = user + system;

        return true;
    }

    /// <summary>
    /// The resident set size from <c>/proc/&lt;pid&gt;/status</c>, a line like <c>VmRSS:   123456 kB</c>.
    /// </summary>
    internal static bool TryParseResidentMemory(string status, out long bytes)
    {
        bytes = 0;

        foreach (var line in status.Split('\n'))
        {
            if (!line.StartsWith("VmRSS:", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length >= 2 && long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var kilobytes))
            {
                bytes = kilobytes * 1024;

                return true;
            }
        }

        return false;
    }
}
