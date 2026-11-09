using System.Collections.Concurrent;
using System.Diagnostics;

namespace OpenWorker.LoadTest.Stats;

/// <summary>
/// The steps a bot goes through to get into the world. Each is counted and timed, so a run shows where the bots
/// that did not get in were stopped.
/// </summary>
internal enum BotStage
{
    /// <summary>The connection to the auth server and the login.</summary>
    Login,

    /// <summary>Asking the auth server which gates there are and which of them to enter.</summary>
    GateList,

    /// <summary>Entering the gate: the connection, the session and the list of characters.</summary>
    GateEnter,

    /// <summary>Creating a character when the account has none, and choosing one.</summary>
    Character,

    /// <summary>Entering the district the gate chose: the connection and the entry of the character.</summary>
    DistrictEnter,

    /// <summary>Travelling to another district, when the run asks for it.</summary>
    DistrictTravel,

    /// <summary>Entering a maze from a district.</summary>
    MazeEnter
}

internal static class BotStages
{
    public static string NameOf(BotStage stage) => stage switch
    {
        BotStage.Login => "auth.login",
        BotStage.GateList => "auth.gate-list",
        BotStage.GateEnter => "gate.enter",
        BotStage.Character => "gate.character",
        BotStage.DistrictEnter => "district.enter",
        BotStage.DistrictTravel => "district.travel",
        BotStage.MazeEnter => "maze.enter",
        _ => stage.ToString()
    };
}

/// <summary>
/// How many bots got through a step, how many were stopped in it, and how long it took those that got through.
/// </summary>
internal sealed class StageStats
{
    private readonly LatencyHistogram _duration = new();
    private readonly Lock _lock = new();

    private long _succeeded;
    private long _failed;

    public long Succeeded => Interlocked.Read(ref _succeeded);

    public long Failed => Interlocked.Read(ref _failed);

    public void Succeed(long startTimestamp)
    {
        lock (_lock)
        {
            _duration.RecordTicks(startTimestamp, Stopwatch.GetTimestamp());
        }

        Interlocked.Increment(ref _succeeded);
    }

    public void Fail()
    {
        Interlocked.Increment(ref _failed);
    }

    public LatencySummary Duration()
    {
        lock (_lock)
        {
            return LatencySummary.Of(_duration);
        }
    }
}

/// <summary>
/// What went wrong, how often, and one example of the message.
/// </summary>
internal sealed class ErrorStat
{
    private long _count;

    public long Count => Interlocked.Read(ref _count);

    public string Example { get; init; } = string.Empty;

    public void Add() => Interlocked.Increment(ref _count);
}

/// <summary>
/// Everything the bots of a run report to one place: how far they got, what stopped them, and the gate that says
/// whether the latency of the moment is being measured.
/// </summary>
internal sealed class RunStats
{
    private readonly Dictionary<BotStage, StageStats> _stages = Enum.GetValues<BotStage>().ToDictionary(e => e, _ => new StageStats());
    private readonly ConcurrentDictionary<string, ErrorStat> _errors = new(StringComparer.Ordinal);
    private readonly List<BotMetrics> _metrics = [];
    private readonly Lock _metricsLock = new();

    private volatile bool _measuring;

    private long _started;
    private long _entering;
    private long _active;
    private long _peakActive;
    private long _inMaze;
    private long _failed;
    private long _lost;
    private long _measureStart;
    private long _measureEnd;

    /// <summary>
    /// Whether latencies are being recorded. The bots check it before they record, so the entry of the bots and the
    /// warm-up do not count.
    /// </summary>
    public bool Measuring => _measuring;

    /// <summary>
    /// Bots started.
    /// </summary>
    public long Started => Interlocked.Read(ref _started);

    /// <summary>
    /// Bots that are still on their way into the world.
    /// </summary>
    public long Entering => Interlocked.Read(ref _entering);

    /// <summary>
    /// Bots in a district or a maze right now.
    /// </summary>
    public long Active => Interlocked.Read(ref _active);

    public long PeakActive => Interlocked.Read(ref _peakActive);

    public long InMaze => Interlocked.Read(ref _inMaze);

    /// <summary>
    /// Bots that were stopped before they got into the world.
    /// </summary>
    public long Failed => Interlocked.Read(ref _failed);

    /// <summary>
    /// Bots that were in the world and lost their connection to the server.
    /// </summary>
    public long Lost => Interlocked.Read(ref _lost);

    public StageStats Stage(BotStage stage) => _stages[stage];

    public IReadOnlyDictionary<string, ErrorStat> Errors => _errors;

    /// <summary>
    /// The time the latencies were recorded for, in seconds.
    /// </summary>
    public double MeasuredSeconds
    {
        get
        {
            var start = Interlocked.Read(ref _measureStart);

            if (start == 0)
            {
                return 0;
            }

            var end = Interlocked.Read(ref _measureEnd);

            return Stopwatch.GetElapsedTime(start, end == 0 ? Stopwatch.GetTimestamp() : end).TotalSeconds;
        }
    }

    public void StartMeasuring()
    {
        Interlocked.Exchange(ref _measureStart, Stopwatch.GetTimestamp());

        _measuring = true;
    }

    public void StopMeasuring()
    {
        if (_measuring)
        {
            _measuring = false;

            Interlocked.Exchange(ref _measureEnd, Stopwatch.GetTimestamp());
        }
    }

    public void Register(BotMetrics metrics)
    {
        lock (_metricsLock)
        {
            _metrics.Add(metrics);
        }
    }

    /// <summary>
    /// The metrics of the bots registered so far.
    /// </summary>
    public BotMetrics[] Bots()
    {
        lock (_metricsLock)
        {
            return [.. _metrics];
        }
    }

    public void BotStarted()
    {
        Interlocked.Increment(ref _started);
        Interlocked.Increment(ref _entering);
    }

    /// <summary>
    /// The bot is in the world and starts its script.
    /// </summary>
    public void BotEntered()
    {
        Interlocked.Decrement(ref _entering);

        var active = Interlocked.Increment(ref _active);
        var peak = Interlocked.Read(ref _peakActive);

        while (active > peak)
        {
            var seen = Interlocked.CompareExchange(ref _peakActive, active, peak);

            if (seen == peak)
            {
                break;
            }

            peak = seen;
        }
    }

    public void BotInMaze(int delta)
    {
        Interlocked.Add(ref _inMaze, delta);
    }

    /// <summary>
    /// The bot was stopped before it got in.
    /// </summary>
    public void BotFailedToEnter()
    {
        Interlocked.Decrement(ref _entering);
        Interlocked.Increment(ref _failed);
    }

    /// <summary>
    /// The bot left the world: the run is ending, or the server dropped it (<paramref name="lost" />).
    /// </summary>
    public void BotLeft(bool lost)
    {
        Interlocked.Decrement(ref _active);

        if (lost)
        {
            Interlocked.Increment(ref _lost);
        }
    }

    /// <summary>
    /// Counts an error under its kind, e.g. <c>auth.login: wrong-password</c>.
    /// </summary>
    public void Error(string stage, string kind, string message)
    {
        var key = stage + ": " + kind;

        _errors.GetOrAdd(key, _ => new ErrorStat { Example = message }).Add();
    }
}
