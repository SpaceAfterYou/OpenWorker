using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.DependencyInjection;

internal sealed partial class ServerService(
    World world,
    WorldLoop loop,
    HandlerExecutor executor,
    IOptions<ServerOptions> server,
    IOptions<GameOptions> game,
    IOptions<HotspotOptions> hotspot,
    IOptions<ShutdownOptions> shutdown,
    ArchComponentProvider components,
    ServerState state,
    SessionTracker sessions,
    IEnumerable<ISessionCloseHandler> closeHandlers,
    SessionMetrics metrics,
    ILogger<ServerService> logger
) :
    BackgroundService
{
    /// <summary>
    /// How long the server waits after a connection that could not be accepted, so a fault that stays does not fill the log.
    /// </summary>
    private static TimeSpan AcceptRetry { get; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How the read loop takes the bytes of a socket: the pipe's own buffers come from the shared pool, and a read waits
    /// for data with a zero-byte read first, so a connection that sends nothing holds no buffer.
    /// </summary>
    private static StreamPipeReaderOptions ReaderOptions { get; } = new(leaveOpen: true, useZeroByteReads: true);

    private const string ClientLeft = "the client closed the connection.";
    private const string ClientLeftInsideAFrame = "the client closed the connection inside a frame.";

    private Lock DumpLock { get; } = new();
    private TcpListener Listener { get; } = new(IPAddress.Any, server.Value.Port);
    private string? DumpDirectory { get; } = ResolveDumpDirectory(hotspot.Value, game.Value);

    /// <summary>
    /// Where Debug builds save every received message, one file per opcode. <c>Hotspot:PacketDumpPath</c>
    /// set to an empty value turns the dumps off.
    /// </summary>
    private static string? ResolveDumpDirectory(HotspotOptions hotspot, GameOptions game)
    {
        return string.IsNullOrWhiteSpace(hotspot.PacketDumpPath)
            ? null
            : Path.Combine(hotspot.PacketDumpPath, game.Version);
    }

    // The drain stops the two halves of a connection on its own, in order: first no new connection, then no new packet.
    private CancellationTokenSource? _accepting;
    private CancellationTokenSource? _reading;
    private Task? _acceptLoop;

    // How long the drain lasts; the sessions that flush their writers are bound by it.
    private CancellationToken _drain;

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _accepting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _reading = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var accepting = _accepting.Token;
        var reading = _reading.Token;

        return _acceptLoop = Task.Run(() => AcceptLoopAsync(cancellationToken, accepting, reading), cancellationToken);
    }

    /// <summary>
    /// The connections that are being served.
    /// </summary>
    public int OpenSessions => sessions.Count;

    /// <summary>
    /// No new connection: the listener is closed when this returns, so a client that connects now is refused.
    /// The sessions that are open go on.
    /// </summary>
    public async Task StopAcceptingAsync()
    {
        if (_accepting is not { } accepting || _acceptLoop is not { } loopTask)
        {
            return;
        }

        await accepting.CancelAsync();

        await loopTask;
    }

    /// <summary>
    /// No new packet, and every session closes through the normal path: the client is told that the server goes
    /// away, the close handlers run on the world loop (the position is saved, the storage cleaned, the lease released),
    /// then the writer sends what is queued and the connection closes. A packet that is being handled is handled to
    /// the end first. Returns when the last session has ended or <paramref name="drain"/> is cancelled, with the number
    /// of sessions that were left.
    /// </summary>
    public async Task<int> CloseSessionsAsync(CancellationToken drain)
    {
        _drain = drain;

        if (_reading is { } reading)
        {
            await reading.CancelAsync();
        }

        try
        {
            await sessions.WhenIdle().WaitAsync(drain);
        }

        catch (OperationCanceledException) when (drain.IsCancellationRequested)
        {
            // The sessions that are left are counted below; the server goes on stopping.
        }

        return sessions.Count;
    }

    public override void Dispose()
    {
        _accepting?.Dispose();
        _reading?.Dispose();

        base.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken stopping, CancellationToken accepting, CancellationToken reading)
    {
        Listener.Start();

        state.MarkListening();

        LogServerReady(logger);

        try
        {
            while (!accepting.IsCancellationRequested)
            {
                TcpClient socket;

                try
                {
                    socket = await Listener.AcceptTcpClientAsync(accepting);
                }

                catch (Exception e) when (e is not OperationCanceledException || !accepting.IsCancellationRequested)
                {
                    // One connection that cannot be accepted (the process is out of file handles, the client went away
                    // at once) must not stop the server from accepting the others.
                    LogAcceptFailed(logger, e);

                    await Task.Delay(AcceptRetry, accepting);

                    continue;
                }

                // Counted before the task starts: the drain waits for every connection that was accepted.
                sessions.Enter();

                _ = Task.Run(() => RunSessionAsync(socket, stopping, reading), CancellationToken.None);
            }
        }

        catch (OperationCanceledException)
        {
            // The server is stopping.
        }

        finally
        {
            LogServerShutdown(logger);

            state.MarkNotListening();

            Listener.Stop();
        }
    }

    private async Task RunSessionAsync(TcpClient socket, CancellationToken stopping, CancellationToken reading)
    {
        try
        {
            await SessionLoopAsync(socket, stopping, reading);
        }

        finally
        {
            sessions.Exit();
        }
    }

    private async Task SessionLoopAsync(TcpClient socket, CancellationToken cancellationToken, CancellationToken reading)
    {
        using (socket)
        {
            // Movement is made of small frames that must not wait for each other.
            socket.NoDelay = true;

            var session = new ServerSessionComponent(socket, metrics);

            // A component built from a socket always has its scope.
            var scope = session.LogScope!;

            var entity = await loop.InvokeAsync(() =>
            {
                var created = components.CreateEntity(world);

                created.Set(session);

                scope.Session = created.Id;

                return created;
            });

            // The lines of the read loop itself. The callbacks of the world loop do not inherit it: each one that is
            // about this session begins the scope again, inside the callback (HandlerExecutor, CloseSessionAsync).
            using var readScope = logger.BeginScope(scope);

            // The reader of the socket ends the connection when the client does, or when the server has already closed it
            // and given its own reason first; the first reason of a connection is the one that counts.
            var reason = SessionCloseReason.Client;

            try
            {
                // The pipe holds what the socket gave and no frame has used yet. A zero-byte read waits for the client
                // without a buffer, so an idle connection holds none.
                var pipe = PipeReader.Create(socket.GetStream(), ReaderOptions);

                try
                {
                    reason = await ReadFramesAsync(pipe, entity, scope, cancellationToken, reading);
                }

                finally
                {
                    await pipe.CompleteAsync();
                }
            }

            catch (InvalidDataException e)
            {
                // Only a header that cannot be right gets here (the executor counts the faults of a body itself). The
                // frames after it cannot be found, so the connection ends at once, with nothing more read.
                LogMalformedHeader(logger, e.Message);

                reason = SessionCloseReason.Malformed;
            }

            catch (Exception e) when (e is EndOfStreamException or IOException { InnerException: SocketException } or ObjectDisposedException or OperationCanceledException)
            {
                LogConnectionClosed(logger, e.Message);

                // The client did not leave: the drain stopped the reading, or the server is stopping.
                if (e is OperationCanceledException)
                {
                    reason = reading.IsCancellationRequested ? SessionCloseReason.Shutdown : SessionCloseReason.Server;
                }
            }

            catch (Exception e)
            {
                LogSessionLoopFailed(logger, e);

                reason = SessionCloseReason.Server;
            }

            finally
            {
                // The server closes this session because it stops, not because the client left or misbehaved.
                var shuttingDown = reading.IsCancellationRequested;

                // Named before the close handlers queue their last messages (the notice of the shutdown first), so a
                // client that cannot take them is still counted under the reason of the close, once, when the writer ends.
                session.SetCloseReason(reason);

                await loop.InvokeAsync(() => CloseSessionAsync(entity, scope, shuttingDown));

                session.Disconnect(reason);

                if (shuttingDown)
                {
                    await FlushAsync(session);
                }
            }
        }
    }

    /// <summary>
    /// Hands the frames of a connection to the world loop, one at a time and in order, until the client leaves
    /// (<see cref="SessionCloseReason.Client"/>) or the drain stops the reading between two frames
    /// (<see cref="SessionCloseReason.Shutdown"/>). A frame whose bytes have not all come waits for more; a header that
    /// cannot be right throws <see cref="InvalidDataException"/>.
    /// </summary>
    private async Task<SessionCloseReason> ReadFramesAsync(PipeReader pipe, Entity entity, SessionLogScope scope,
        CancellationToken cancellationToken, CancellationToken reading)
    {
        // The drain cancels `reading` and not `cancellationToken`: the frame that is being handled is handled to the
        // end, and only then does the loop stop reading.
        while (!reading.IsCancellationRequested)
        {
            var result = await pipe.ReadAsync(reading);
            var buffer = result.Buffer;

            try
            {
                while (!reading.IsCancellationRequested && TryReadFrame(ref buffer, out var header, out var body))
                {
                    await HandleFrameAsync(header, body, entity, scope, cancellationToken);
                }
            }

            finally
            {
                // What is left is the start of a frame: kept, and looked at again only when more bytes come.
                pipe.AdvanceTo(buffer.Start, buffer.End);
            }

            if (result.IsCompleted)
            {
                LogConnectionClosed(logger, buffer.IsEmpty ? ClientLeft : ClientLeftInsideAFrame);

                return SessionCloseReason.Client;
            }
        }

        return SessionCloseReason.Shutdown;
    }

    /// <summary>
    /// Takes the first whole frame off the front of <paramref name="buffer"/>. False, with the buffer as it was, when
    /// the bytes of the frame have not all come.
    /// </summary>
    private static bool TryReadFrame(ref ReadOnlySequence<byte> buffer, out MessageHeader header, out ReadOnlySequence<byte> body)
    {
        header = default;
        body = default;

        if (buffer.Length < MessageHeader.HeaderSize)
        {
            return false;
        }

        Span<byte> head = stackalloc byte[MessageHeader.HeaderSize];

        buffer.Slice(0, MessageHeader.HeaderSize).CopyTo(head);

        // Checked as soon as it is there: a client that claims a frame it cannot have is not waited for.
        header = new MessageHeader(head);

        var length = MessageHeader.HeaderSize + header.ContentLength;

        if (buffer.Length < length)
        {
            return false;
        }

        body = buffer.Slice(MessageHeader.HeaderSize, header.ContentLength);
        buffer = buffer.Slice(length);

        return true;
    }

    /// <summary>
    /// Decrypts one frame in a buffer of its own, rented for the size of the frame, and hands it to the world loop.
    /// The buffer goes back to the pool when the handler has finished with it.
    /// </summary>
    private async Task HandleFrameAsync(MessageHeader header, ReadOnlySequence<byte> body, Entity entity, SessionLogScope scope,
        CancellationToken cancellationToken)
    {
        var length = header.ContentLength;
        var rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            var buffer = rented.AsMemory(0, length);

            body.CopyTo(buffer.Span);

            metrics.Received(MessageHeader.HeaderSize + length);

            MessageEncryption.Exchange(buffer.Span, header.Version);

            DumpPacket(header, buffer);

            await loop.InvokeAsync(() => executor.ExecuteAsync(buffer, entity, scope, cancellationToken));
        }

        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Runs on the world loop, as every close does: the close handlers of the modules, then the end of the entity.
    /// The lines of the close handlers carry the scope of the session, begun here, inside the callback of the loop.
    /// </summary>
    private async ValueTask CloseSessionAsync(Entity entity, SessionLogScope scope, bool shuttingDown)
    {
        using var logScope = logger.BeginScope(scope);

        if (shuttingDown)
        {
            NotifyShuttingDown(entity);
        }

        foreach (var handler in closeHandlers)
        {
            try
            {
                await handler.OnSessionClosedAsync(entity);
            }

            catch (Exception e)
            {
                LogCloseHandlerFailed(logger, e);
            }
        }

        world.Destroy(entity);
    }

    /// <summary>
    /// Tells the client that the server is going away, as the first thing of the close, so that it is in the queue
    /// before the close handlers (which may take a while) run. The client knows the message already: the reason
    /// is one of its own, the one it shows for a server that is shut down.
    /// </summary>
    private void NotifyShuttingDown(Entity entity)
    {
        if (!world.TryGet(entity, out ServerSessionComponent session))
        {
            return;
        }

        // The client is not necessarily logged in: zero is what the login servers send for an account they do not know.
        world.TryGet(entity, out ClaimsComponent claims);

        session.Send(new PersonKickOutResponse
        {
            OutReason = PersonKickOutReason.GmToolServerShutdown,
            Account = claims.Account
        });
    }

    /// <summary>
    /// Waits until the client has everything the server queued for it, and closes the connection when it does not
    /// within the time of the drain: a client that does not read would hold the shutdown otherwise.
    /// </summary>
    private async Task FlushAsync(ServerSessionComponent session)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(_drain);

        limit.CancelAfter(shutdown.Value.DrainTimeout);

        try
        {
            await session.Closed.WaitAsync(limit.Token);
        }

        catch (OperationCanceledException)
        {
            LogFlushTimedOut(logger);

            session.Abort();
        }
    }

    [Conditional("DEBUG")]
    private void DumpPacket(MessageHeader header, Memory<byte> buffer)
    {
        if (DumpDirectory is not { } dir || buffer.Length < 2)
        {
            return;
        }

        var name = $"0x{buffer.Span[0]:X2},0x{buffer.Span[1]:X2}.bin";
        var path = Path.Join(dir, name);

        // The dumps end up in the repository: passwords and tickets must not.
        var content = buffer.ToArray();

        PacketRedaction.Redact(content);

        try
        {
            // Connections read at the same time and share the file of an opcode.
            lock (DumpLock)
            {
                Directory.CreateDirectory(dir);

                using var file = File.Open(path, FileMode.Create);
                using var writer = new BinaryWriter(file);

                header.ToBinary(writer);

                writer.Write(content);
            }
        }

        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LogDumpFailed(logger, e, path);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Server ready.")]
    private static partial void LogServerReady(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Server shutdown.")]
    private static partial void LogServerShutdown(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not accept a connection.")]
    private static partial void LogAcceptFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection closed: {Reason}")]
    private static partial void LogConnectionClosed(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The client sent a header that cannot be right, and is cut off: {Reason}")]
    private static partial void LogMalformedHeader(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Session loop failed.")]
    private static partial void LogSessionLoopFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Session close handler failed.")]
    private static partial void LogCloseHandlerFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to save packet {Path}.")]
    private static partial void LogDumpFailed(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A client did not take what was queued for it within the drain and is cut off.")]
    private static partial void LogFlushTimedOut(ILogger logger);
}
