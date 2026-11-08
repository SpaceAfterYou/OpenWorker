using System.Buffers;
using System.Diagnostics;
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

            var entity = await loop.InvokeAsync(() =>
            {
                var created = components.CreateEntity(world);

                created.Set(session);

                return created;
            });

            // The reader of the socket ends the connection when the client does, or when the server has already closed it
            // and given its own reason first; the first reason of a connection is the one that counts.
            var reason = SessionCloseReason.Client;

            try
            {
                using var memory = MemoryPool<byte>.Shared.Rent(MessageHeader.MaxContentLength);

                var stream = socket.GetStream();
                var head = new byte[MessageHeader.HeaderSize];

                // The drain cancels `reading` and not `cancellationToken`: the packet that is being handled is
                // handled to the end, and only then does the loop stop reading.
                while (!reading.IsCancellationRequested)
                {
                    await stream.ReadExactlyAsync(head, reading);

                    var header = new MessageHeader(head);
                    var buffer = memory.Memory[..header.ContentLength];

                    await stream.ReadExactlyAsync(buffer, reading);

                    metrics.Received(MessageHeader.HeaderSize + header.ContentLength);

                    MessageEncryption.Exchange(buffer, header.Version);

                    DumpPacket(header, buffer);

                    await loop.InvokeAsync(() => executor.ExecuteAsync(buffer, entity, cancellationToken));
                }

                // The loop ends without an error only when the drain has stopped the reading between two packets.
                reason = SessionCloseReason.Shutdown;
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

                await loop.InvokeAsync(() => CloseSessionAsync(entity, shuttingDown));

                session.Disconnect(reason);

                if (shuttingDown)
                {
                    await FlushAsync(session);
                }
            }
        }
    }

    /// <summary>
    /// Runs on the world loop, as every close does: the close handlers of the modules, then the end of the entity.
    /// </summary>
    private async ValueTask CloseSessionAsync(Entity entity, bool shuttingDown)
    {
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

    [LoggerMessage(Level = LogLevel.Critical, Message = "Session loop failed.")]
    private static partial void LogSessionLoopFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Session close handler failed.")]
    private static partial void LogCloseHandlerFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to save packet {Path}.")]
    private static partial void LogDumpFailed(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A client did not take what was queued for it within the drain and is cut off.")]
    private static partial void LogFlushTimedOut(ILogger logger);
}
