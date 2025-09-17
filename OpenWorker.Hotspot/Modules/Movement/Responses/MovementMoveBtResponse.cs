using System.Numerics;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

[HotspotMessage(Group, Command)]
public readonly struct MovementMoveBtResponse(Arch.Core.World world, Arch.Core.Entity creature) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.MoveBt;

    public Vector3 fPos { get; init; }
    public float fYaw { get; init; }
    public float fTargetPosX { get; init; }
    public float fTargetPosY { get; init; }
    public byte byRunBit { get; init; }
    public float fPitch { get; init; }
    public float fMoveSpeed { get; init; }
    public byte byChangeMotion { get; init; }

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.WriteActor(world.Get<ActorComponent>(creature));
        writer.WriteMapValue(world.Get<WorldComponent>(creature).Map);
        writer.Write(fPos);
        writer.Write(fYaw);
        writer.Write(fTargetPosX);
        writer.Write(fTargetPosY);
        writer.Write(byRunBit);
        writer.Write(fPitch);
        writer.Write(fMoveSpeed);
        writer.Write(byChangeMotion);
    }
}