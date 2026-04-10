using System.Numerics;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillPositionValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public Vector3 Position { get; init; } = reader.ReadVector3();
    public float Angle { get; init; } = reader.ReadSingle();
    public short MotionClass { get; init; } = reader.ReadInt16();

    /// <summary>
    /// TODO: Client has a field but skips when read.
    /// </summary>
    // public bool IsSwapSkill { get; init; } = reader.ReadBoolean();

    public bool HasInputFlag { get; init; } = reader.ReadBoolean();

#endregion Message: Body

#region Interface: IWritableData


    public void Write(BinaryWriter writer)
    {
        writer.Write(Position);
        writer.Write(Angle);
        writer.Write(MotionClass);

        // TODO: Client has a field but skips when read.
        // writer.Write(IsSwapSkill);

        writer.Write(HasInputFlag);
    }

#endregion Interface: IWritableData
}

