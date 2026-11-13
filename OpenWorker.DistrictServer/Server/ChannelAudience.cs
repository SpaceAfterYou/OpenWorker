using System.Numerics;
using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Server.Common;
using OpenWorker.Wire;

namespace OpenWorker.DistrictServer.Server;

/// <summary>
/// In a district the chat goes to the whole channel of the player, and everything else to those who see the player: the
/// cells of the visibility grid of its channel around it.
/// </summary>
public sealed class ChannelAudience(World world, ServiceChannels channels) : IPlayerAudience
{
    public void Chat<T>(Entity player, in T message) where T : IResponseHotspotMessage
    {
        channels.Get(player).Broadcast(in message);
    }

    public void Nearby<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable
    {
        channels.Get(player).BroadcastNearby(player, in message);
    }

    public void NearbyToOthers<T>(Entity player, in T message) where T : IResponseHotspotMessage
    {
        channels.Get(player).BroadcastNearbyToOthers(player, in message);
    }

    public void Move(Entity player, in MovementMoveBtResponse message)
    {
        channels.Get(player).Move(player, in message);
    }

    public void Relocate(Entity player, Vector3 position)
    {
        channels.Get(player).Relocate(player, position, world);
    }
}
