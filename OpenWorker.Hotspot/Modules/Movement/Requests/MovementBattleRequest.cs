using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Requests;

[HotspotMessage(Group, Command)]
public readonly struct MovementBattleRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.Battle;

    public ActorValue dwActorID { get; } = new(reader);
    public float fPosX { get; } = reader.ReadSingle();
    public float fPosY { get; } = reader.ReadSingle();
    public float fPosZ { get; } = reader.ReadSingle();
    public float fYaw { get; } = reader.ReadSingle();
    public byte bBattlePose { get; } = reader.ReadByte(); // TODO: Name
    public int bPlayMotion { get; } = reader.ReadByte(); // TODO: Name
    public MessageOpcode Opcode => new(Group, Command);
}