using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Movement.Requests;

[HotspotMessage(Group, Command)]
public readonly struct MovementJumpRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.Jump;

    public ActorValue dwActorID { get; } = new(reader);
    public MapValue nMapID { get; } = reader.ReadMapValue();
    public float fPosX { get; } = reader.ReadSingle();
    public float fPosY { get; } = reader.ReadSingle();
    public float fPosZ { get; } = reader.ReadSingle();
    public float fYaw { get; } = reader.ReadSingle();
    public float fTargetPosX { get; } = reader.ReadSingle();
    public float fTargetPosY { get; } = reader.ReadSingle();
    public byte bJumpingMove { get; } = reader.ReadByte();
    public byte bJumpDrop { get; } = reader.ReadByte();

    public MessageOpcode Opcode => new(Group, Command);
}