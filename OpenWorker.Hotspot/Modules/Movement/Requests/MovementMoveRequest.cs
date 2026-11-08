using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Requests;

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly partial struct MovementMoveRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.Move;

    public ActorValue dwActorID { get; }
    public MapValue nMapID { get; }
    public float fPosX { get; }
    public float fPosY { get; }
    public float fPosZ { get; }
    public float fYaw { get; }
    public float fTargetPosX { get; }
    public float fTargetPosY { get; }
    public byte byRunBit { get; }
    public float fPitch { get; }
    public float fMoveSpeed { get; }
    public byte byChangeMotion { get; }

    public MessageOpcode Opcode => new(Group, Command);
}
