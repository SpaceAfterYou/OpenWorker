using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

public readonly struct SkillDeckValue(BinaryReader reader) : IWritableData
{
    public ushort Position { get; init; } = reader.ReadUInt16();

    public int Skill1 { get; init; } = reader.ReadInt32();
    public int Skill2 { get; init; } = reader.ReadInt32();
    public int Skill3 { get; init; } = reader.ReadInt32();
    public int Skill4 { get; init; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Position);

        writer.Write(Skill1);
        writer.Write(Skill2);
        writer.Write(Skill3);
        writer.Write(Skill4);
    }
}
