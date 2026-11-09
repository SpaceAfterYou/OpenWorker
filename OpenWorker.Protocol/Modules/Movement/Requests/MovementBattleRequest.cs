using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MovementBattleRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.Battle;

    public ActorValue dwActorID { get; }
    public float fPosX { get; }
    public float fPosY { get; }
    public float fPosZ { get; }
    public float fYaw { get; }
    public byte bBattlePose { get; } // TODO: Name
    [WireAs(typeof(byte))]
    public int bPlayMotion { get; } // TODO: Name
    public MessageOpcode Opcode => new(Group, (byte)Command);
}