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
    HandlerExecutor executor,
    IConfiguration configuration,
    ArchComponentProvider components,
    ILogger<ServerService> logger
) :
    BackgroundService
{
    private TcpListener Listener { get; } = new(IPAddress.Any, 10_000);
    private string GameVersion { get; } = configuration.GetValue("Game:Version", "0.0.0.0");

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(
            () => AcceptLoopAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        Listener.Start();

        logger.LogDebug("Server ready.");

        while (cancellationToken.IsCancellationRequested is false)
        {
            var socket = await Listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

            _ = Task.Factory.StartNew(
                () => SessionLoopAsync(socket, cancellationToken).ConfigureAwait(false),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        logger.LogDebug("Server shutdown.");

        Listener.Stop();
    }

    private async Task SessionLoopAsync(TcpClient socket, CancellationToken cancellationToken)
    {
        using (socket)
        {
            var entity = world.Create(components.GetComponents());

            entity.Set(new ServerSessionComponent(socket));

            try
            {
                using var memory = MemoryPool<byte>.Shared.Rent(short.MaxValue);

                var stream = socket.GetStream();

                var reader = new BinaryReader(stream);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var header = new MessageHeader(reader);
                    var buffer = memory.Memory[..header.ContentLength];

                    await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);

                    MessageEncryption.Exchange(buffer, header.Version);

                    DumpPacket(header, buffer);

                    await executor.ExecuteAsync(buffer, entity, cancellationToken).ConfigureAwait(false);
                }
            }

            catch (Exception e)
            {
                logger.LogCritical(e, "Session loop failed.");
            }

            finally
            {
                world.Destroy(entity);
            }
        }
    }

    [Conditional("DEBUG")]
    private void DumpPacket(MessageHeader header, Memory<byte> buffer)
    {
        var dir = Path.Combine("test", "hotspot", GameVersion);
        Directory.CreateDirectory(dir);

        var name = $"0x{buffer.Span[0]:X2},0x{buffer.Span[1]:X2}.bin";
        var path = Path.Join(dir, name);

        using var file = File.Open(path, FileMode.Create);
        using var writer = new BinaryWriter(file);

        header.ToBinary(writer);

        writer.Write(buffer.Span);
    }
}
