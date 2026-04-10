using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillDeckValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public ushort Position { get; init; } = reader.ReadUInt16();

    public int Skill1 { get; init; } = reader.ReadInt32();
    public int Skill2 { get; init; } = reader.ReadInt32();
    public int Skill3 { get; init; } = reader.ReadInt32();
    public int Skill4 { get; init; } = reader.ReadInt32();

#endregion Message: Body

    public IEnumerable<int> GetList()
    {
        yield return Skill1;
        yield return Skill2;
        yield return Skill3;
        yield return Skill4;
    }

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Position);

        writer.Write(Skill1);
        writer.Write(Skill2);
        writer.Write(Skill3);
        writer.Write(Skill4);
    }

#endregion Interface: IWritableData
}
