using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Hotspot;

/// <summary>
/// The one thread that runs the game logic of a server. World is not thread safe, so packets,
/// connections opening and closing and periodic work all run here, one at a time.<br/>
/// Waiting for the database or Redis releases the thread, and the code continues here afterwards.
/// That only works while the await captures this context, so server code must not use
/// <c>ConfigureAwait(false)</c>, and slow synchronous work must go through <c>Task.Run</c>.<br/>
/// Other connections run while a handler waits: see the rules in <c>ARCHITECTURE.md</c>.
/// </summary>
public sealed class WorldLoop : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly ILogger<WorldLoop> _logger;
    private readonly Thread _thread;

    public WorldLoop(ILogger<WorldLoop> logger)
    {
        _logger = logger;
        _thread = new Thread(Run) { IsBackground = true, Name = nameof(WorldLoop) };
        _thread.Start();
    }

    public bool IsCurrent => Environment.CurrentManagedThreadId == _thread.ManagedThreadId;

    public override void Post(SendOrPostCallback callback, object? state)
    {
        _queue.Add((callback, state));
    }

    public override void Send(SendOrPostCallback callback, object? state)
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

    private void Run()
    {
        SetSynchronizationContext(this);

        foreach (var (callback, state) in _queue.GetConsumingEnumerable())
        {
            try
            {
                callback(state);
            }

            catch (Exception e)
            {
                // One broken continuation must not stop the whole server.
                _logger.LogCritical(e, "World loop callback failed.");
            }
        }
    }
}
