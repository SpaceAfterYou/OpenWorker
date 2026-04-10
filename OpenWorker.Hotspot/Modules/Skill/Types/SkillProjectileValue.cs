using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillProjectileValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public int Skill { get; init; } = reader.ReadInt32();
    public short TriggerIndex { get; init; } = reader.ReadInt16();
    public Vector3 Position { get; init; } = reader.ReadVector3();
    public Vector3 Direction { get; init; } = reader.ReadVector3();
    public uint Session { get; init; } = reader.ReadUInt32();
    public ActorValue Target { get; init; } = new(reader);

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Skill);
        writer.Write(TriggerIndex);
        writer.Write(Position);
        writer.Write(Direction);
        writer.Write(Session);
        writer.Write(Target);
    }

#endregion Interface: IWritableData
}
