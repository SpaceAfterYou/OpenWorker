using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MovementJumpBtResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.JumpBt;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorComponent Actor { get; init; }
    public MapValue MapId { get; init; }

    public float PosX { get; init; }
    public float PosY { get; init; }
    public float PosZ { get; init; }
    public float Yaw { get; init; }
    public float TargetPosX { get; init; }
    public float TargetPosY { get; init; }
    public byte JumpingMove { get; init; }
    public byte JumpDrop { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.WriteMapValue(MapId);
        writer.Write(PosX);
        writer.Write(PosY);
        writer.Write(PosZ);
        writer.Write(Yaw);
        writer.Write(TargetPosX);
        writer.Write(TargetPosY);
        writer.Write(JumpingMove);
        writer.Write(JumpDrop);
    }

#endregion Interface: IWritableData
}
