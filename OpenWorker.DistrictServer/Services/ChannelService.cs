using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Channel;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Hotspot.Modules.Channels.Requests;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.World.Types;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class ChannelService(
    World world,
    NpcManager npcManager,
    ServiceChannels serviceChannels,
    ServerChannels serverChannels
) :
    IHotspotHandler<ChannelInfoRequest>,
    IHotspotHandler<ChannelChangeRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChannelChangeRequest request)
    {
        await serverChannels
            .SwitchAsync(context.GetPlayerEntity(), request.Channel)
            .ConfigureAwait(false);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChannelInfoRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());

        await serverChannels.SendListAsync(context.GetPlayerEntity(), context.CancellationToken).ConfigureAwait(false);

        var channel = world.Get<ChannelMemberComponent>(context.GetPlayerEntity());
        var serviceChannel = serviceChannels[channel.Index];
        
        var npcWireList = npcManager.Collection
            .Select(e =>
            {
                var a = e.Get<ActorComponent>();
                var n = e.Get<CreatureComponent___Old>();
                return new WorldNpcListEntry(
                    a,
                    n.Position,
                    n.Rotation,
                    n.Health,
                    n.Waypoint,
                    n.Sector,
                    n.Level,
                    n.Prototype);
            })
            .ToArray();

        session.Send(new WorldOtherInfosNpcResponse { List = npcWireList });
        
        serviceChannel.SendOthers(context.GetPlayerEntity(), world);
    }
}