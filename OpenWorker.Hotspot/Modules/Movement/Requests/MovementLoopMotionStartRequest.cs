using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct MovementLoopMotionStartRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.LoopMotionStart;

    public ActorValue dwActorID { get; } = new(reader);
    public float fPosX { get; } = reader.ReadSingle();
    public float fPosY { get; } = reader.ReadSingle();
    public float fPosZ { get; } = reader.ReadSingle();
    public float fYaw { get; } = reader.ReadSingle();
    public int dwStartAniID { get; } = reader.ReadInt32();
    public int dwLoopAniID { get; } = reader.ReadInt32();
    public int dwEndAniID { get; } = reader.ReadInt32();
    public int dwIdleAniID { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}
