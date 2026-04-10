using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillInfoValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public int Skill { get; init; } = reader.ReadInt32();
    public int Divergence { get; init; } = reader.ReadInt32();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Skill);
        writer.Write(Divergence);
    }

#endregion Interface: IWritableData
}
