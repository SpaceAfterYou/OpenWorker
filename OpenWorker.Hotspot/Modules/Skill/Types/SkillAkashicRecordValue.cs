using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillAkashicRecordValue(BinaryReader reader) : IWritableData
{
    public uint Akashic { get; init; } = reader.ReadUInt32();
    public ActorValue Player { get; init; } = new(reader);
    public SkillPositionValue SkillPosition { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Akashic);
        writer.Write(Player);

        SkillPosition.Write(writer);
    }
};
