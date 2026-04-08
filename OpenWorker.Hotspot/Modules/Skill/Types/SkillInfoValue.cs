using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

public readonly struct SkillInfoValue(BinaryReader reader) : IWritableData
{
    public int Skill { get; init; } = reader.ReadInt32();
    public int Divergence { get; init; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Skill);
        writer.Write(Divergence);
    }
}
