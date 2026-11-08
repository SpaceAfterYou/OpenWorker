namespace OpenWorker.DependencyInjection;

/// <summary>
/// Counts the connections that are being served, so that a server that stops can wait for the last one. A connection
/// is entered when it is accepted, before its task starts, and exited when its task ends: whatever has been
/// accepted when the listener closes is counted, however far its task has come.
/// </summary>
internal sealed class SessionTracker
{
    private readonly Lock _lock = new();

    private int _active;
    private TaskCompletionSource? _idle;

    /// <summary>
    /// The connections that have been accepted and have not ended.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _active;
            }
        }
    }

    public void Enter()
    {
        lock (_lock)
        {
            _active++;
        }
    }

    public void Exit()
    {
        lock (_lock)
        {
            if (--_active == 0)
            {
                _idle?.TrySetResult();
                _idle = null;
            }
        }
    }

    /// <summary>
    /// Completes when no connection is being served. A connection accepted later is not waited for: stop accepting first.
    /// </summary>
    public Task WhenIdle()
    {
        lock (_lock)
        {
            if (_active == 0)
            {
                return Task.CompletedTask;
            }

            _idle ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            return _idle.Task;
        }
    }
}
