using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenWorker.Infrastructure.Communication.Hotspot.Handlers;
using SoulWorkerResearch.SoulCore.IO.Net;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Packet;

internal sealed class PacketExecutor
{
    private readonly HotSpotSession _session;
    private readonly HandlerCollection _handlers;

    private readonly ILogger _logger;

    public PacketExecutor(HotSpotSession session, HandlerCollection handlers, ILogger<PacketExecutor> logger)
    {
        _session = session;
        _handlers = handlers;

        _logger = logger;
    }

    internal async ValueTask Invoke(BinaryReader reader, CancellationToken ct)
    {
        if (!_session.Entity.IsEnabled())
        {
            _logger.LogWarning("Session is disabled");
            return;
        }

        if (!_session.IsConnected)
        {
            _logger.LogWarning("Session is disconnected");
            return;
        }

        var handler = _handlers[new Opcode(reader)];

        await using var scope = _session.Entity.Get<AsyncServiceScope>().ServiceProvider.CreateAsyncScope();
        var instance = scope.ServiceProvider.GetRequiredService(handler.Class);

        try
        {
            await handler.Method(instance, _session.Entity, reader, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shit happens");
            _session.Disconnect();
        }
    }
}
