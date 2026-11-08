using System.Net.Sockets;
using System.Threading.Channels;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot;

/// <summary>
/// Sends the frames of one connection in order on its own task, so that a client that has stopped
/// reading delays nobody but itself.<br/>
/// It holds a reference to every <see cref="Frame"/> in its queue and lets it go once the frame is written or dropped.<br/>
/// It is also where the end of the connection is counted: the write loop ends exactly once, and the first
/// <see cref="SessionCloseReason"/> given to the writer is the one reported.
/// </summary>
internal sealed class SessionWriter
{
    private const int MaxQueuedFrames = 4096;

    private static TimeSpan WriteTimeout { get; } = TimeSpan.FromSeconds(10);

    // A connection is a socket, or (in the benchmarks and tests) a stream that stands for one.
    private readonly TcpClient? _client;
    private readonly Stream? _stream;
    private readonly SessionMetrics? _metrics;

    private readonly Channel<Frame> _queue = Channel.CreateBounded<Frame>(
        new BoundedChannelOptions(MaxQueuedFrames) { SingleReader = true });

    // A SessionCloseReason, set by the first writer to claim it. Zero is no reason yet.
    private int _closeReason;

    // Set once the stream that stands for a connection is closed.
    private int _streamClosed;

    /// <param name="metrics">Counts the connection; <see langword="null"/> for a connection nobody measures.</param>
    public SessionWriter(TcpClient client, SessionMetrics? metrics = null)
        : this(client, null, metrics)
    {
    }

    /// <summary>
    /// A connection that is only a stream: it is open until the writer closes it.
    /// </summary>
    public SessionWriter(Stream stream, SessionMetrics? metrics = null)
        : this(null, stream, metrics)
    {
    }

    private SessionWriter(TcpClient? client, Stream? stream, SessionMetrics? metrics)
    {
        _client = client;
        _stream = stream;
        _metrics = metrics;

        _metrics?.Opened();

        Completed = Task.Run(WriteLoopAsync);
    }

    public bool IsOpen => _client?.Connected ?? Volatile.Read(ref _streamClosed) == 0;

    /// <summary>
    /// Completes when the write loop has ended: everything queued is sent (or dropped, for a client that went away)
    /// and the connection is closed. The loop answers the errors of a connection itself, so it ends quietly.
    /// </summary>
    public Task Completed { get; }

    /// <summary>
    /// Queues the frame with a reference of its own, which the writer lets go after the frame is written or dropped. A frame
    /// that cannot be queued is let go at once.
    /// </summary>
    public bool TryEnqueue(Frame frame)
    {
        frame.AddReference();

        if (IsOpen && _queue.Writer.TryWrite(frame))
        {
            _metrics?.Queued();

            return true;
        }

        frame.Release();

        // A client this far behind will not catch up; a connection that is already gone has its reason already.
        Close(IsOpen ? SessionCloseReason.Backpressure : SessionCloseReason.Client);

        return false;
    }

    /// <summary>
    /// Sends what is already queued and closes the connection.
    /// </summary>
    public void Complete(SessionCloseReason reason)
    {
        SetCloseReason(reason);

        _queue.Writer.TryComplete();
    }

    /// <summary>
    /// Closes the connection now and drops what is still queued: for a client that does not take what was queued
    /// within the time a drain allows. A connection that has no reason yet is one whose client does not read.
    /// </summary>
    public void Abort()
    {
        Close(SessionCloseReason.Backpressure);
    }

    /// <summary>
    /// Keeps the reason if the connection already has one: the first reason is the cause, the rest are its consequences.
    /// </summary>
    public void SetCloseReason(SessionCloseReason reason)
    {
        Interlocked.CompareExchange(ref _closeReason, (int)reason, (int)SessionCloseReason.None);
    }

    private void Close(SessionCloseReason reason)
    {
        SetCloseReason(reason);

        _queue.Writer.TryComplete();

        if (_client is not null)
        {
            _client.Close();
        }

        else if (Interlocked.Exchange(ref _streamClosed, 1) == 0)
        {
            _stream!.Dispose();
        }
    }

    private async Task WriteLoopAsync()
    {
        // One source for every write: a write that ends in time leaves it as it was, so no frame makes a source or a timer.
        var timeout = new CancellationTokenSource();

        try
        {
            await foreach (var frame in _queue.Reader.ReadAllAsync())
            {
                _metrics?.Dequeued();

                try
                {
                    timeout.CancelAfter(WriteTimeout);

                    await (_client?.GetStream() ?? _stream!).WriteAsync(frame.Memory, timeout.Token);

                    _metrics?.Sent(frame.Length);
                }

                finally
                {
                    frame.Release();
                }

                // The time ran out just as the write ended: the next write needs a source that is not cancelled.
                if (!timeout.TryReset())
                {
                    timeout.Dispose();
                    timeout = new CancellationTokenSource();
                }
            }
        }

        catch (Exception e) when (e is IOException or ObjectDisposedException or InvalidOperationException or OperationCanceledException)
        {
            // The reader of this connection reports the disconnect. The write that timed out is a client that does not read.
            SetCloseReason(e is OperationCanceledException ? SessionCloseReason.Backpressure : SessionCloseReason.Client);
        }

        finally
        {
            timeout.Dispose();

            Finish();
        }
    }

    /// <summary>
    /// The end of the write loop, whatever ended it: closes the socket, drops what was not sent and counts the connection closed.
    /// </summary>
    private void Finish()
    {
        // Every way to complete the queue names its reason, so one is missing only when the loop itself failed.
        Close(SessionCloseReason.Server);

        while (_queue.Reader.TryRead(out var frame))
        {
            _metrics?.Dequeued();

            frame.Release();
        }

        _metrics?.Closed((SessionCloseReason)Volatile.Read(ref _closeReason));
    }
}
