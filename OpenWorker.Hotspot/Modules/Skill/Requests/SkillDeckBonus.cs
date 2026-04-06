using OpenWorker.Extensions;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

public readonly struct SkillDeckBonus
{
    public short[] Value { get; } = new short[4];

    public SkillDeckBonus(BinaryReader reader)
    {
        Value = reader.ReadUInt16AsArray(4);
    }
}