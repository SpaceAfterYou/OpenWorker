using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Enums;
using OpenWorker.Hotspot.Modules.Skill.Extensions;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillLearnResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillLearn;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public SkillInfoValue Info { get; init; } = new(reader);

    /// <summary>
    /// TODO: Research, always zero.
    /// </summary>
    public SkillType Type { get; init; } = reader.ReadSkillType();

    public bool IsSuccessful { get; init; } = reader.ReadBoolean();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Info.Write(writer);

        writer.Write(Type);
        writer.Write(IsSuccessful);
    }

#endregion Interface: IWritableData
}
