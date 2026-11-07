using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

/// <summary>
/// A monster circles its target or stops doing so.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MovementGazeBtResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.GazeBt;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public Vector3 Position { get; init; }
    public float Yaw { get; init; }
    public bool Gazing { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Position);
        writer.Write(Yaw);
        writer.Write(Gazing ? 1 : 0);
    }
}
