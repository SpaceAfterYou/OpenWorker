using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct MovementMotionRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.Motion;

    public ActorValue dwActorID { get; } = new(reader);
    public short nMotionClass { get; } = reader.ReadInt16();
    public short nSubClass { get; } = reader.ReadInt16();
    
    public MessageOpcode Opcode => new(Group, Command);
}