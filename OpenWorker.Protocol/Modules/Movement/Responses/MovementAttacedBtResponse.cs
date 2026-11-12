using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

/// <summary>
/// An actor is carried by another one: it keeps its place before the carrier, in the direction and at the distance given, until
/// <see cref="MovementAttacedEndBtResponse"/>.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MovementAttacedBtResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.AttacedBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// The one that carries.
    /// </summary>
    public ActorValue Owner { get; init; }

    /// <summary>
    /// The one that is carried.
    /// </summary>
    public ActorValue Actor { get; init; }

    public Vector3 Direction { get; init; }

    public float Distance { get; init; }
}
