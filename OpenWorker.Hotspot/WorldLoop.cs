using System.Collections.Concurrent;
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

    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly ILogger<WorldLoop> _logger;
    private readonly WorldLoopMetrics? _metrics;
    private readonly Thread _thread;

    /// <param name="metrics">Reports the queue and the callbacks; <see langword="null"/> for a loop nobody measures.</param>
    public WorldLoop(ILogger<WorldLoop> logger, WorldLoopMetrics? metrics = null)
    {
        _logger = logger;
        _metrics = metrics;
        _metrics?.ObserveQueue(QueueLength);
        _thread = new Thread(Run) { IsBackground = true, Name = nameof(WorldLoop) };
        _thread.Start();
    }

    public bool IsCurrent => Environment.CurrentManagedThreadId == _thread.ManagedThreadId;

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

        // The queue cannot be disposed while the thread still reads it.
        if (!IsCurrent && _thread.Join(StopTimeout))
        {
            _queue.Dispose();
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

            _metrics?.Finish(started);
        }
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "World loop callback failed.")]
    private partial void LogCallbackFailed(Exception exception);
}
