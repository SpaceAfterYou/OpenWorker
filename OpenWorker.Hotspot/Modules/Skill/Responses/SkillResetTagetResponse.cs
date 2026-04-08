using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>
/// Client <c>sub_95D980</c>: <c>sub_41C560</c> fills four dwords (stack <c>v7[0]..v7[3]</c>), then <c>ushort</c> → <c>word_108E980</c>.
/// Branching: if <c>v7[1]</c> ≠ 0 then <c>sub_630800(flt_108E3C0, v8, v9)</c>; else if <c>!v7[0] || v8</c> then <c>sub_630390(..., v8)</c>; else <c>sub_6306B0(..., v7[0])</c>.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillResetTagetResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillResetTaget;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>Used on the “clear slot” path via <c>sub_6306B0</c> when the pair-update branch is not taken.</summary>
    public int SkillGroup { get; init; } = reader.ReadInt32();

    /// <summary>Non-zero selects <c>sub_630800</c> with <see cref="BoundSkillId"/> and <see cref="BoundDivergenceId"/>.</summary>
    public int BoundSkillPairUpdate { get; init; } = reader.ReadInt32();

    /// <summary>Skill id fed to <c>sub_630800</c> / <c>sub_630390</c> depending on branch.</summary>
    public int BoundSkillId { get; init; } = reader.ReadInt32();

    /// <summary>Second argument to <c>sub_630800</c> (divergence / slot payload).</summary>
    public int BoundDivergenceId { get; init; } = reader.ReadInt32();

    /// <summary>Written to client <c>word_108E980</c> (UI skill points display).</summary>
    public ushort SkillPointsDisplay { get; init; } = reader.ReadUInt16();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(SkillGroup);
        writer.Write(BoundSkillPairUpdate);
        writer.Write(BoundSkillId);
        writer.Write(BoundDivergenceId);
        writer.Write(SkillPointsDisplay);
    }

#endregion Interface: IWritableData
}
