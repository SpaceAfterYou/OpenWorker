using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct ProjectileShotValue(BinaryReader reader) : IWritableData
{
    public int Skill { get; init; } = reader.ReadInt32();
    public short Attack { get; init; } = reader.ReadInt16();
    public Vector3 Position { get; init; } = reader.ReadVector3();
    public Vector3 Direction { get; init; } = reader.ReadVector3();
    public Vector3 Right { get; init; } = reader.ReadVector3();
    public float Yaw { get; init; } = reader.ReadSingle();
    public float Pitch { get; init; } = reader.ReadSingle();
    public short AdditionalDirectionX { get; init; } = reader.ReadInt16();
    public short AdditionalDirectionY { get; init; } = reader.ReadInt16();
    public uint Session { get; init; } = reader.ReadUInt32();
    public ActorValue Target { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Skill);
        writer.Write(Attack);
        writer.Write(Position);
        writer.Write(Direction);
        writer.Write(Right);
        writer.Write(Yaw);
        writer.Write(Pitch);
        writer.Write(AdditionalDirectionX);
        writer.Write(AdditionalDirectionY);
        writer.Write(Session);
        writer.Write(Target);
    }
}
