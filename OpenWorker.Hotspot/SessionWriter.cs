using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;

namespace OpenWorker.Hotspot;

/// <summary>
/// Sends the frames of one connection in order on its own task, so that a client that has stopped
/// reading delays nobody but itself.
/// </summary>
internal sealed class SessionWriter
{
    private const int MaxQueuedFrames = 4096;

    private static TimeSpan WriteTimeout { get; } = TimeSpan.FromSeconds(10);

    private readonly TcpClient _client;

    private readonly Channel<(byte[] Buffer, int Length)> _queue = Channel.CreateBounded<(byte[], int)>(
        new BoundedChannelOptions(MaxQueuedFrames) { SingleReader = true });

    public SessionWriter(TcpClient client)
    {
        _client = client;

        _ = Task.Run(WriteLoopAsync);
    }

    public bool IsOpen => _client.Connected;

    /// <summary>
    /// Takes over the buffer, which must come from the shared array pool.
    /// </summary>
    public bool TryEnqueue(byte[] buffer, int length)
    {
        if (IsOpen && _queue.Writer.TryWrite((buffer, length)))
        {
            return true;
        }

        ArrayPool<byte>.Shared.Return(buffer);

        // A client this far behind will not catch up.
        Close();

        return false;
    }

    /// <summary>
    /// Sends what is already queued and closes the connection.
    /// </summary>
    public void Complete()
    {
        _queue.Writer.TryComplete();
    }

    private void Close()
    {
        _queue.Writer.TryComplete();
        _client.Close();
    }

    private async Task WriteLoopAsync()
    {
        try
        {
            await foreach (var (buffer, length) in _queue.Reader.ReadAllAsync())
            {
                try
                {
                    using var timeout = new CancellationTokenSource(WriteTimeout);

                    await _client.GetStream().WriteAsync(buffer.AsMemory(0, length), timeout.Token);
                }

                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        catch (Exception e) when (e is IOException or ObjectDisposedException or InvalidOperationException or OperationCanceledException)
        {
            // The reader of this connection reports the disconnect.
        }

        Close();

        while (_queue.Reader.TryRead(out var frame))
        {
            ArrayPool<byte>.Shared.Return(frame.Buffer);
        }
    }
}
