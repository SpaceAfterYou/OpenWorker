using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillProjectileRemoveResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.ProjectileRemove;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public uint Session { get; init; } = reader.ReadUInt32();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Session);
    }

#endregion Interface: IWritableData
}
