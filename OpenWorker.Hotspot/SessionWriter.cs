using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot;

/// <summary>
/// Sends the frames of one connection in order on its own task, so that a client that has stopped
/// reading delays nobody but itself.<br/>
/// It is also where the end of the connection is counted: the write loop ends exactly once, and the first
/// <see cref="SessionCloseReason"/> given to the writer is the one reported.
/// </summary>
internal sealed class SessionWriter
{
    private const int MaxQueuedFrames = 4096;

    private static TimeSpan WriteTimeout { get; } = TimeSpan.FromSeconds(10);

    private readonly TcpClient _client;
    private readonly SessionMetrics? _metrics;

    private readonly Channel<(byte[] Buffer, int Length)> _queue = Channel.CreateBounded<(byte[], int)>(
        new BoundedChannelOptions(MaxQueuedFrames) { SingleReader = true });

    // A SessionCloseReason, set by the first writer to claim it. Zero is no reason yet.
    private int _closeReason;

    /// <param name="metrics">Counts the connection; <see langword="null"/> for a connection nobody measures.</param>
    public SessionWriter(TcpClient client, SessionMetrics? metrics = null)
    {
        _client = client;
        _metrics = metrics;

        _metrics?.Opened();

        Completed = Task.Run(WriteLoopAsync);
    }

    public bool IsOpen => _client.Connected;

    /// <summary>
    /// Completes when the write loop has ended: everything queued is sent (or dropped, for a client that went away)
    /// and the connection is closed. The loop answers the errors of a connection itself, so it ends quietly.
    /// </summary>
    public Task Completed { get; }

    /// <summary>
    /// Takes over the buffer, which must come from the shared array pool.
    /// </summary>
    public bool TryEnqueue(byte[] buffer, int length)
    {
        if (IsOpen && _queue.Writer.TryWrite((buffer, length)))
        {
            _metrics?.Queued();

            return true;
        }

        ArrayPool<byte>.Shared.Return(buffer);

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
        _client.Close();
    }

    private async Task WriteLoopAsync()
    {
        try
        {
            await foreach (var (buffer, length) in _queue.Reader.ReadAllAsync())
            {
                _metrics?.Dequeued();

                try
                {
                    using var timeout = new CancellationTokenSource(WriteTimeout);

                    await _client.GetStream().WriteAsync(buffer.AsMemory(0, length), timeout.Token);

                    _metrics?.Sent(length);
                }

                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
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

            ArrayPool<byte>.Shared.Return(frame.Buffer);
        }

        _metrics?.Closed((SessionCloseReason)Volatile.Read(ref _closeReason));
    }
}
