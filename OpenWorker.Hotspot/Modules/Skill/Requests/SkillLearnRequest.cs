using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct SkillLearnRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillLearn;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required SkillInfoValue Info { get; init; } = new(reader);

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Info.Write(writer);
    }

#endregion Interface: IWritableData
}
