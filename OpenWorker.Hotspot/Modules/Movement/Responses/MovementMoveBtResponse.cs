using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly partial struct MovementMoveBtResponse : IResponseHotspotMessage
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
}
