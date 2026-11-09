using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot;

/// <summary>
/// The one thread that runs the game logic of a server. World is not thread safe, so packets,
/// connections opening and closing and periodic work all run here, one at a time.<br/>
/// Waiting for the database or Redis releases the thread, and the code continues here afterwards.
/// That only works while the await captures this context, so server code must not use
/// <c>ConfigureAwait(false)</c>, and slow synchronous work must go through <c>Task.Run</c>.<br/>
/// Other connections run while a handler waits: see the rules in <c>ARCHITECTURE.md</c>.
/// </summary>
public sealed partial class WorldLoop : SynchronizationContext, IDisposable
{
    /// <summary>
    /// How long <see cref="Dispose"/> waits for the thread to finish what is queued.
    /// </summary>
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The longest a pass may last while the queue never empties, before what waits for its end runs anyway.
    /// </summary>
    private static readonly long MaxPassTicks = Stopwatch.Frequency / 200;

    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly ILogger<WorldLoop> _logger;
    private readonly WorldLoopMetrics? _metrics;
    private readonly Thread _thread;

    // What waits for the end of the pass, and when the first of it was added. Touched on the loop thread only.
    private readonly List<IEndOfPass> _endOfPass = [];
    private long _passStarted;

    /// <param name="metrics">Reports the queue and the callbacks; <see langword="null"/> for a loop nobody measures.</param>
    /// <param name="sessions">Counts the messages the encoder of the loop drops; <see langword="null"/> for none.</param>
    public WorldLoop(ILogger<WorldLoop> logger, WorldLoopMetrics? metrics = null, SessionMetrics? sessions = null)
    {
        _logger = logger;
        _metrics = metrics;
        Encoder = new FrameEncoder(logger, sessions);
        _metrics?.ObserveQueue(QueueLength);
        _thread = new Thread(Run) { IsBackground = true, Name = nameof(WorldLoop) };
        _thread.Start();
    }

    public bool IsCurrent => Environment.CurrentManagedThreadId == _thread.ManagedThreadId;

    /// <summary>
    /// Encodes the messages sent on the loop, one after another: the loop has one thread, so one encoder.
    /// </summary>
    internal FrameEncoder Encoder { get; }

    /// <summary>
    /// Callbacks that wait for the thread. Safe to read from any thread, for the gauge.
    /// </summary>
    private int QueueLength()
    {
        try
        {
            return _queue.Count;
        }

        catch (ObjectDisposedException)
        {
            // The loop has been stopped and its queue is gone.
            return 0;
        }
    }

    /// <summary>
    /// Runs the work once the loop has handled everything queued now, before it waits for more: at the end of the pass.
    /// What arrives meanwhile joins the pass, so work that many callbacks add to (the moves of one actor) is done once for
    /// all of them. A pass that never runs dry ends after 5 ms anyway. Call it on the loop; work added twice in one pass
    /// runs twice, so the caller remembers that it has added it.
    /// </summary>
    public void AtEndOfPass(IEndOfPass work)
    {
        Debug.Assert(IsCurrent, "The end of a pass belongs to the loop thread.");

        if (_endOfPass.Count == 0)
        {
            _passStarted = Stopwatch.GetTimestamp();
        }

        _endOfPass.Add(work);
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        try
        {
            _queue.Add((d, state));
        }

        catch (Exception e) when (e is InvalidOperationException or ObjectDisposedException)
        {
            // The loop has been stopped by Dispose: a continuation that arrives now has nowhere to run.
        }
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        throw new NotSupportedException("Waiting for the loop can block it forever, use InvokeAsync.");
    }

    public override SynchronizationContext CreateCopy()
    {
        return this;
    }

    public Task InvokeAsync(Action action)
    {
        return InvokeAsync(() =>
        {
            action();

            return ValueTask.CompletedTask;
        });
    }

    public Task<T> InvokeAsync<T>(Func<T> function)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        Post(_ =>
        {
            try
            {
                completion.SetResult(function());
            }

            catch (Exception e)
            {
                completion.SetException(e);
            }
        }, null);

        return completion.Task;
    }

    public Task InvokeAsync(Func<ValueTask> function)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Post(_ => _ = RunAsync(function, completion), null);

        return completion.Task;
    }

    private static async Task RunAsync(Func<ValueTask> function, TaskCompletionSource completion)
    {
        try
        {
            await function();

            completion.SetResult();
        }

        catch (Exception e)
        {
            completion.SetException(e);
        }
    }

    /// <summary>
    /// Stops the loop: what is queued still runs, then the thread ends and later posts are dropped.
    /// Does not wait when called from the loop itself, and gives up waiting after <see cref="StopTimeout"/>
    /// on a callback that never returns.
    /// </summary>
    public void Dispose()
    {
        _queue.CompleteAdding();

        // The queue cannot be disposed while the thread still reads it, nor the encoder while the thread may still send.
        if (!IsCurrent && _thread.Join(StopTimeout))
        {
            _queue.Dispose();
            Encoder.Dispose();
        }
    }

    private void Run()
    {
        SetSynchronizationContext(this);

        foreach (var (callback, state) in _queue.GetConsumingEnumerable())
        {
            var started = _metrics?.Start() ?? 0;

            try
            {
                callback(state);
            }

            catch (Exception e)
            {
                // One broken continuation must not stop the whole server.
                LogCallbackFailed(e);
            }

            if (_endOfPass.Count > 0 && (_queue.Count == 0 || Stopwatch.GetTimestamp() - _passStarted >= MaxPassTicks))
            {
                EndPass();
            }

            _metrics?.Finish(started);
        }
    }

    private void EndPass()
    {
        // Work may add more work; it runs in the same round.
        for (var index = 0; index < _endOfPass.Count; index++)
        {
            try
            {
                _endOfPass[index].EndOfPass();
            }

            catch (Exception e)
            {
                LogCallbackFailed(e);
            }
        }

        _endOfPass.Clear();
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "World loop callback failed.")]
    private partial void LogCallbackFailed(Exception exception);
}

/// <summary>
/// Work that waits for the end of a pass of the world loop (<see cref="WorldLoop.AtEndOfPass"/>).
/// </summary>
public interface IEndOfPass
{
    void EndOfPass();
}
