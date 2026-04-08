using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct SkillProjectileRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.ProjectileReq;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Skill { get; init; } = reader.ReadInt32();
    public short TriggerIndex { get; init; } = reader.ReadInt16();
    public Vector3 Position { get; init; } = reader.ReadVector3();
    public Vector3 Direction { get; init; } = reader.ReadVector3();
    public Vector3 Right { get; init; } = reader.ReadVector3();
    public float Yaw { get; init; } = reader.ReadSingle();
    public float Pitch { get; init; } = reader.ReadSingle();
    public short AdditionalDirectionX { get; init; } = reader.ReadInt16();
    public short AdditionalDirectionY { get; init; } = reader.ReadInt16();
    public uint Session { get; init; } = reader.ReadUInt32();
    public ActorValue Target { get; init; } = new(reader);

#endregion Message: Body
}
