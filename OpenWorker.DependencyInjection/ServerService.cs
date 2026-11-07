using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.DependencyInjection;

internal sealed class ServerService(
    World world,
    WorldLoop loop,
    HandlerExecutor executor,
    IConfiguration configuration,
    ArchComponentProvider components,
    IEnumerable<ISessionCloseHandler> closeHandlers,
    ILogger<ServerService> logger
) :
    BackgroundService
{
    private Lock DumpLock { get; } = new();
    private TcpListener Listener { get; } = new(IPAddress.Any, 10_000);
    private string GameVersion { get; } = configuration.GetValue("Game:Version", "0.0.0.0");

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => AcceptLoopAsync(cancellationToken), cancellationToken);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        Listener.Start();

        logger.LogDebug("Server ready.");

        while (cancellationToken.IsCancellationRequested is false)
        {
            var socket = await Listener.AcceptTcpClientAsync(cancellationToken);

            _ = Task.Run(() => SessionLoopAsync(socket, cancellationToken), cancellationToken);
        }

        logger.LogDebug("Server shutdown.");

        Listener.Stop();
    }

    private async Task SessionLoopAsync(TcpClient socket, CancellationToken cancellationToken)
    {
        using (socket)
        {
            // Movement is made of small frames that must not wait for each other.
            socket.NoDelay = true;

            var session = new ServerSessionComponent(socket);

            var entity = await loop.InvokeAsync(() =>
            {
                var created = components.CreateEntity(world);

                created.Set(session);

                return created;
            });

            try
            {
                using var memory = MemoryPool<byte>.Shared.Rent(MessageHeader.MaxContentLength);

                var stream = socket.GetStream();
                var head = new byte[MessageHeader.HeaderSize];

                while (!cancellationToken.IsCancellationRequested)
                {
                    await stream.ReadExactlyAsync(head, cancellationToken);

                    var header = new MessageHeader(head);
                    var buffer = memory.Memory[..header.ContentLength];

                    await stream.ReadExactlyAsync(buffer, cancellationToken);

                    MessageEncryption.Exchange(buffer, header.Version);

                    DumpPacket(header, buffer);

                    await loop.InvokeAsync(() => executor.ExecuteAsync(buffer, entity, cancellationToken));
                }
            }

            catch (Exception e) when (e is EndOfStreamException or IOException { InnerException: SocketException } or ObjectDisposedException or OperationCanceledException)
            {
                logger.LogDebug("Connection closed: {Reason}", e.Message);
            }

            catch (Exception e)
            {
                logger.LogCritical(e, "Session loop failed.");
            }

            finally
            {
                await loop.InvokeAsync(() => CloseSessionAsync(entity));

                session.Disconnect();
            }
        }
    }

    private async ValueTask CloseSessionAsync(Entity entity)
    {
        foreach (var handler in closeHandlers)
        {
            try
            {
                await handler.OnSessionClosedAsync(entity);
            }

            catch (Exception e)
            {
                logger.LogError(e, "Session close handler failed.");
            }
        }

        world.Destroy(entity);
    }

    [Conditional("DEBUG")]
    private void DumpPacket(MessageHeader header, Memory<byte> buffer)
    {
        var dir = Path.Combine("test", "hotspot", GameVersion);
        var name = $"0x{buffer.Span[0]:X2},0x{buffer.Span[1]:X2}.bin";
        var path = Path.Join(dir, name);

        try
        {
            // Connections read at the same time and share the file of an opcode.
            lock (DumpLock)
            {
                Directory.CreateDirectory(dir);

                using var file = File.Open(path, FileMode.Create);
                using var writer = new BinaryWriter(file);

                header.ToBinary(writer);

                writer.Write(buffer.Span);
            }
        }

        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(e, "Failed to save packet {Path}.", path);
        }
    }
}
