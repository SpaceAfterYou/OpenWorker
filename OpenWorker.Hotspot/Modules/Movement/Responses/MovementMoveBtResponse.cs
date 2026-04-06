using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MovementMoveBtResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.MoveBt;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public MapValue Map { get; init; }
    public Vector3 Pos { get; init; }
    public float Yaw { get; init; }
    public float TargetPosX { get; init; }
    public float TargetPosY { get; init; }
    public byte RunBit { get; init; }
    public float Pitch { get; init; }
    public float MoveSpeed { get; init; }
    public byte ChangeMotion { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.WriteMapValue(Map);
        writer.Write(Pos);
        writer.Write(Yaw);
        writer.Write(TargetPosX);
        writer.Write(TargetPosY);
        writer.Write(RunBit);
        writer.Write(Pitch);
        writer.Write(MoveSpeed);
        writer.Write(ChangeMotion);
    }
}
