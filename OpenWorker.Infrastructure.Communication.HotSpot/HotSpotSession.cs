using DefaultEcs;
using Microsoft.Extensions.DependencyInjection;
using NetCoreServer;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Channels.Abstractions;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Services;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages;
using System.Diagnostics;

namespace OpenWorker.Infrastructure.Communication.Hotspot;

internal sealed class HotSpotSession : TcpSession, IDisposable, IHotSpotSession
{
    public Entity Entity { get; }

    internal CancellationToken CancellationToken => _cts.Token;
    internal IServiceProvider ServiceProvider => Entity.Get<AsyncServiceScope>().ServiceProvider;

    private readonly CancellationTokenSource _cts;

    private readonly IWriteOnlyRawPacketPipe _packetChannel;

    public HotSpotSession(HotSpotServer server, World world, IWriteOnlyRawPacketPipe packetChannel) : base(server)
    {
        Entity = world.CreateEntity();
        // Entity.Set<IHotSpotSession>(this);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(server.CancellationToken);

        _packetChannel = packetChannel;
    }

    protected override void OnConnected()
    {
        Entity.Enable();
        Entity.Get<AsyncServiceScope>().ServiceProvider.GetRequiredService<PacketHandlerService>().StartAsync(CancellationToken).Wait(CancellationToken);
    }

    protected override void OnDisconnected()
    {
        Entity.Disable();

        var scope = Entity.Get<AsyncServiceScope>();

        scope.ServiceProvider.GetRequiredService<PacketHandlerService>().StopAsync(CancellationToken).Wait(CancellationToken);
        scope.DisposeAsync().AsTask().Wait(CancellationToken);

        _cts.Cancel();
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        Debug.Assert(offset <= int.MaxValue);
        Debug.Assert(size <= int.MaxValue);

        _packetChannel.Write(buffer.AsSpan((int)offset, (int)size));
    }

    public ValueTask<bool> SendAsync<T>(T message, CancellationToken ct = default) where T : IBinaryOutcomingMessage
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposingManagedResources)
    {
        if (disposingManagedResources)
        {
            _cts.Dispose();
            Entity.Dispose();
        }

        base.Dispose(disposingManagedResources);
    }
}
