using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillBuffDamageBroadcastResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.BuffDamageBt;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorValue Target { get; init; } = new(reader);
    public short Buff { get; init; } = reader.ReadInt16();
    public int Damage { get; init; } = reader.ReadInt32();
    public int Health { get; init; } = reader.ReadInt32();
    public ActorValue Owner { get; init; } =  new(reader);

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Target);
        writer.Write(Buff);
        writer.Write(Damage);
        writer.Write(Health);
        writer.Write(Owner);
    }

#endregion Interface: IWritableData
}
