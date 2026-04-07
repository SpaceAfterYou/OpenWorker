using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

public readonly struct ActiveSkillValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public ActorValue Actor { get; init; } = new(reader);
    public int Skill { get; init; } = reader.ReadInt32();
    public SkillPositionValue Position { get; init; } = new(reader);
    public int Divergence { get; init; } = reader.ReadInt32();
    public int ParentSkill { get; init; } = reader.ReadInt32();
    public byte SkillDeckIndex { get; init; } = reader.ReadByte();
    public int RandomTrapIndex { get; init; } = reader.ReadInt32();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(Skill);

        Position.Write(writer);

        writer.Write(Divergence);
        writer.Write(ParentSkill);
        writer.Write(SkillDeckIndex);
        writer.Write(RandomTrapIndex);
    }

    #endregion Interface: IWritableData
}
