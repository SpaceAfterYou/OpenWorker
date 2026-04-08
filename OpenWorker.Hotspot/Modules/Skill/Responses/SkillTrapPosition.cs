using System.Numerics;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

public readonly struct SkillTrapPosition(BinaryReader reader) : IWritableData
{
    public int Skill { get; init; } = reader.ReadInt16();
    public short TriggerIndex { get; init; } = reader.ReadInt16();
    public Vector3 Position { get; init; } = reader.ReadVector3();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Skill);
        writer.Write(TriggerIndex);
        writer.Write(Position);
    }
}
