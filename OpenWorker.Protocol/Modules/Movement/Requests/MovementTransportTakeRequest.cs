using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Requests;

/// <summary>
/// The player asks the NPC of transports for a ride: its own actor, the row of the ride among the rides of the NPC
/// (<c>tb_Transport_Info</c>) and the actor of the NPC.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MovementTransportTakeRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.TransportTake;

    public ActorValue Actor { get; }
    public short Row { get; }
    public ActorValue Npc { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}