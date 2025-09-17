using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

[HotspotMessage(Group, Command)]
public readonly struct MovementJumpBtResponse(ActorComponent actor, MapValue nMapID) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.JumpBt;

    public MessageOpcode Opcode => new(Group, Command);

    public float fPosX { get; init; }
    public float fPosY { get; init; }
    public float fPosZ { get; init; }
    public float fYaw { get; init; }
    public float fTargetPosX { get; init; }
    public float fTargetPosY { get; init; }
    public byte bJumpingMove { get; init; }
    public byte bJumpDrop { get; init; }

    public void ToBinary(BinaryWriter writer)
    {
        writer.WriteActor(actor);
        writer.WriteMapValue(nMapID);
        writer.Write(fPosX);
        writer.Write(fPosY);
        writer.Write(fPosZ);
        writer.Write(fYaw);
        writer.Write(fTargetPosX);
        writer.Write(fTargetPosY);
        writer.Write(bJumpingMove);
        writer.Write(bJumpDrop);
    }
}