using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Extensions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillPointValue(BinaryReader reader) : IWritableData
{
    public short TotalSkillPoint { get; init; } = reader.ReadInt16();
    public short SkillPoint { get; init; } = reader.ReadInt16();

    public void Write(BinaryWriter writer)
    {
        writer.Write(TotalSkillPoint);
        writer.Write(SkillPoint);
    }
}

public readonly struct SkillSnapshotValue(BinaryReader reader) : IWritableData
{
    public ActorValue Target { get; init; } = new(reader);
    public SkillPointValue SkillPoint { get; init; } = new(reader);
    public short DeckSlotCount { get; init; } = reader.ReadInt16();
    public ushort[] DeckBonus { get; init; } = reader.ReadSkillDeckBonus();
    public SkillInfoValue[] SkillList { get; init; } = reader.ReadSkillInfoValueList();
    public SkillDeckValue[] SkillDeck { get; init; } = reader.ReadSkillDeckValueList();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Target);

        SkillPoint.Write(writer);

        writer.Write(DeckSlotCount);
        writer.WriteSkillDeckBonus(DeckBonus);
        writer.Write(SkillList);
        writer.Write(SkillDeck);
    }
}
