using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct MovementAttacedEndRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.AttacedEnd;

    public ActorValue dwActorID { get; } = new(reader);
    public float fPosX { get; } = reader.ReadSingle();
    public float fPosY { get; } = reader.ReadSingle();
    public float fPosZ { get; } = reader.ReadSingle();

    public MessageOpcode Opcode => new(Group, Command);
}
