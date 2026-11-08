using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.DistrictServer.Services;

public sealed class MovementService(World world, ServiceChannels channels) : MovementServiceBase(world)
{
    protected override void Broadcast(Entity player, IResponseHotspotMessage message)
    {
        channels.Get(player).Broadcast(message);
    }

    protected override void BroadcastToOthers(Entity player, IResponseHotspotMessage message)
    {
        channels.Get(player).BroadcastToOthers(player, message);
    }
}
