using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Requests;

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly partial struct MovementMotionRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.Motion;

    public ActorValue dwActorID { get; }
    public short nMotionClass { get; }
    public short nSubClass { get; }
    
    public MessageOpcode Opcode => new(Group, Command);
}