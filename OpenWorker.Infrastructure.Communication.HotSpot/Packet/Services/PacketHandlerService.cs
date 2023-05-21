using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Channels.Abstractions;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Packet.Services;

internal sealed class PacketHandlerService : BackgroundService
{
    private readonly HotSpotSession _session;

    private readonly PacketReader _packetReader;
    private readonly PacketExecutor _packetExecutor;

    private readonly IReadOnlyRawPacketPipe _pipe;
    private readonly ILogger _logger;

    public PacketHandlerService(HotSpotSession session, PacketReader packetReader, PacketExecutor packetExecutor, IReadOnlyRawPacketPipe pipe, ILogger<PacketHandlerService> logger)
    {
        _session = session;

        _packetReader = packetReader;
        _packetExecutor = packetExecutor;

        _pipe = pipe;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var stream = _pipe.OpenStream();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var bodyReader = _packetReader.OpenBodyReader(stream);
                await _packetExecutor.Invoke(bodyReader, ct);
            }
            catch (InvalidOperationException) 
            {
                // Stream was closed OR read after stream end's
                // [ 1st ] - good. Session disconnected
                // [ 2nd ] - bad. Kick needed. IDK how to check what happens
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shit happens");
                _session.Disconnect();
                break;
            }
        }
    }
}
