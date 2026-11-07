using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

/// <summary>
/// An actor is held at a place.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MovementGrapResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.Grap;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public Vector3 Position { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Position);
    }
}
