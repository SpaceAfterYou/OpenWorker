using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Extensions;
using OpenWorker.Hotspot.Modules.Skill.Requests;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillLoadDeckResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillLoadDeck;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public SkillDeckValue[] SkillDeckList { get; init; } = reader.ReadSkillDeckValueList();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(SkillDeckList);
    }

#endregion Interface: IWritableData
}
