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
using OpenWorker.Hotspot.Modules.World.Requests;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.World.Types;

namespace OpenWorker.DistrictServer.Services;

public sealed class ChannelService(
    World world,
    NpcManager npcManager,
    ServiceChannels serviceChannels,
    ServerChannels serverChannels
) :
    IHotspotHandler<WorldObjectRequest>,
    IHotspotHandler<ChannelInfoRequest>,
    IHotspotHandler<ChannelChangeRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ChannelChangeRequest request)
    {
        serverChannels.Switch(context.Player, request.Channel);

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChannelInfoRequest request)
    {
        await serverChannels.SendListAsync(context.Player, context.CancellationToken);
    }

    /// <summary>
    /// The client asks what is around it when it has finished loading a district or changed the channel.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, WorldObjectRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var channel = world.Get<ChannelMemberComponent>(context.Player);

        var npcWireList = npcManager.Collection
            .Select(e =>
            {
                var a = e.Get<ActorComponent>();
                var n = e.Get<NpcComponent>();
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

        serviceChannels[channel.Index].SendOthers(context.Player, world);

        return ValueTask.CompletedTask;
    }
}
