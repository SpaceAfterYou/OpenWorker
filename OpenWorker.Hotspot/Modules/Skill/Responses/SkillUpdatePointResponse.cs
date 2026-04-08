using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>
/// Client <c>sub_95D170</c> / <c>sub_40AF70</c>: two int16; assigned to <c>word_108E982</c> then <c>word_108E980</c>.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillUpdatePointResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillUpdatePoint;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>Stored in client <c>word_108E982</c> (first on wire).</summary>
    public short SkillUiWord982 { get; init; } = reader.ReadInt16();

    /// <summary>Stored in client <c>word_108E980</c> — primary skill-point display total.</summary>
    public short SkillUiWord980 { get; init; } = reader.ReadInt16();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(SkillUiWord982);
        writer.Write(SkillUiWord980);
    }

#endregion Interface: IWritableData
}
