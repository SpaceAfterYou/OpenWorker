using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

/// <summary>
/// A player rides: the client of the rider locks its input and plays the path, the clients of the others play it on the
/// rider. The client reads the three fields it asked with; it ends the ride with <see cref="MovementTransportOffResponse"/>.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MovementTransportTakeResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.TransportTake;

    public required ActorValue Actor { get; init; }
    public required short Row { get; init; }
    public required ActorValue Npc { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}