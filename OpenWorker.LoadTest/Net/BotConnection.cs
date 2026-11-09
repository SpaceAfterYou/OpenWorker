using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using OpenWorker.LoadTest.Protocol;

namespace OpenWorker.LoadTest.Net;

/// <summary>
/// Who a connection hands the frames it reads to.
/// </summary>
internal interface IFrameSink
{
    /// <summary>
    /// A frame came: its body, opcode first, decrypted. Called on the read loop of the connection, one frame after
    /// another, so it must not wait. The bytes are valid only during the call. <paramref name="receivedAt" /> is the
    /// <see cref="Stopwatch" /> reading taken when the bytes were read from the socket.
    /// </summary>
    void OnFrame(ReadOnlySpan<byte> frame, long receivedAt);

    /// <summary>
    /// The connection ended: the server closed it, or something broke. Called once, after the last frame.
    /// </summary>
    void OnClosed(Exception? error);
}

/// <summary>
/// A TCP connection of a bot to a server. One task reads it for as long as it lives, into one buffer that is
/// reused for every frame, and hands each frame to the sink. Sending is allowed from any task, one at a time.
/// </summary>
internal sealed class BotConnection : IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly IFrameSink _sink;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _closing = new();

    private Task _reading = Task.CompletedTask;
    private int _closed;

    private BotConnection(Socket socket, IFrameSink sink)
    {
        _socket = socket;
        _sink = sink;
    }

    public bool IsClosed => Volatile.Read(ref _closed) != 0;

    /// <summary>
    /// Ends when the read loop does, after the sink was told.
    /// </summary>
    public Task Completion => _reading;

    /// <summary>
    /// Opens a connection and starts reading it. Gives up after <paramref name="timeout" />.
    /// </summary>
    public static async Task<BotConnection> ConnectAsync(string host, int port, IFrameSink sink, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);

        if (addresses.Length == 0)
        {
            throw new SocketException((int)SocketError.HostNotFound);
        }

        var socket = new Socket(addresses[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            // A move is a small frame that must not wait to be joined by the next one.
            NoDelay = true
        };

        try
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            limit.CancelAfter(timeout);

            try
            {
                await socket.ConnectAsync(addresses[0], port, limit.Token);
            }

            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"No connection to {host}:{port} within {timeout}.");
            }
        }

        catch
        {
            socket.Dispose();

            throw;
        }

        var connection = new BotConnection(socket, sink);

        connection._reading = connection.ReadLoopAsync();

        return connection;
    }

    /// <summary>
    /// Sends the bytes, which hold one frame or several. Waits its turn behind a send that is still going.
    /// </summary>
    public async ValueTask SendAsync(ReadOnlyMemory<byte> bytes)
    {
        await _sendLock.WaitAsync(_closing.Token);

        try
        {
            while (!bytes.IsEmpty)
            {
                var sent = await _socket.SendAsync(bytes, SocketFlags.None, _closing.Token);

                bytes = bytes[sent..];
            }
        }

        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Closes the connection, waits for the read loop to end, and frees the socket.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Close();

        await _reading;

        _socket.Dispose();
        _closing.Dispose();
        _sendLock.Dispose();
    }

    private void Close()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

        try
        {
            _closing.Cancel();
        }

        catch (ObjectDisposedException)
        {
            // Already gone.
        }

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }

        catch (Exception e) when (e is SocketException or ObjectDisposedException)
        {
            // The other end closed first, or the socket was never fully open.
        }
    }

    private async Task ReadLoopAsync()
    {
        // Leave the thread of the caller before the first read, so a connect that completed inline does not
        // run the whole loop in the middle of the code that opened it.
        await Task.Yield();

        Exception? error = null;

        using var frames = new FrameBuffer();

        try
        {
            while (true)
            {
                var received = await _socket.ReceiveAsync(frames.ReceiveMemory(), SocketFlags.None, _closing.Token);

                if (received == 0)
                {
                    break;
                }

                frames.Advance(received);

                Dispatch(frames, Stopwatch.GetTimestamp());
            }
        }

        catch (OperationCanceledException)
        {
            // Closed on purpose.
        }

        catch (Exception e)
        {
            error = e;
        }

        Close();

        _sink.OnClosed(error);
    }

    private void Dispatch(FrameBuffer frames, long receivedAt)
    {
        while (frames.TryReadFrame(out var frame))
        {
            _sink.OnFrame(frame, receivedAt);
        }
    }
}
