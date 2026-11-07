using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MovementIgnoreMotionDeltaResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.IgnoreMotionDelta;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public Vector3 Pos { get; init; }
    public float Yaw { get; init; }
    public float Pitch { get; init; }
    public byte Forced { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Pos);
        writer.Write(Yaw);
        writer.Write(Pitch);
        writer.Write(Forced);
    }
}
