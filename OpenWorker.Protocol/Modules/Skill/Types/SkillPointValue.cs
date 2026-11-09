using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillPointValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public short TotalSkillPoint { get; init; } = reader.ReadInt16();
    public short FreeSkillPoint { get; init; } = reader.ReadInt16();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(TotalSkillPoint);
        writer.Write(FreeSkillPoint);
    }

#endregion Interface: IWritableData
}
