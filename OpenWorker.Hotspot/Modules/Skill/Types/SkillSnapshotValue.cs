using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Extensions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillSnapshotValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public ActorValue Target { get; init; } = new(reader);
    public SkillPointValue SkillPoint { get; init; } = new(reader);
    public short DeckSlotCount { get; init; } = reader.ReadInt16();
    public ushort[] DeckBonus { get; init; } = reader.ReadSkillDeckBonusList();
    public SkillInfoValue[] SkillList { get; init; } = reader.ReadSkillInfoValueList();
    public SkillDeckValue[] DeckSlotList { get; init; } = reader.ReadSkillDeckValueList();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Target);

        SkillPoint.Write(writer);

        writer.Write(DeckSlotCount);
        writer.WriteSkillDeckBonus(DeckBonus);
        writer.Write(SkillList);
        writer.Write(DeckSlotList);
    }

#endregion Interface: IWritableData
}
