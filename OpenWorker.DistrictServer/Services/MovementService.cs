using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
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
