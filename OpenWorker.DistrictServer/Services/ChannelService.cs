using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Responses;
using OpenWorker.Hotspot.Modules.Channels;
using OpenWorker.Hotspot.Modules.Channels.Components;
using OpenWorker.Hotspot.Modules.Channels.Requests;
using OpenWorker.Hotspot.Modules.World.Responses;

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
            .SwitchAsync(context.Player, request.Channel)
            .ConfigureAwait(false);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChannelInfoRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        await serverChannels.SendListAsync(context.Player, context.CancellationToken).ConfigureAwait(false);

        var channel = world.Get<ChannelMemberComponent>(context.Player);
        var serviceChannel = serviceChannels[channel.Index];
        
        session.Send(new WorldOtherInfosNpcResponse(npcManager.Collection));
        
        serviceChannel.SendOthers(context.Player, world);
    }
}