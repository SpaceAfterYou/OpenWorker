using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillDmgValue(BinaryReader reader) : IWritableData
{
    public ActorValue UxActorId { get; init; } = new(reader);

    public bool IsDamageType { get; init; } = reader.ReadBoolean();

    public bool IsDamageTarget { get; init; } = reader.ReadBoolean();

    public int Damage { get; init; } = reader.ReadInt32();

    public int Hp { get; init; } = reader.ReadInt32();

    public Vector3 ExtraMove { get; init; } = reader.ReadVector3();

    public float FlySpeed { get; init; } = reader.ReadSingle();

    public byte AttackCounter { get; init; } = reader.ReadByte();

    public byte DefenseType { get; init; } = reader.ReadByte();

    public float CurSuperArmorGauge { get; init; } = reader.ReadSingle();

    public byte HitPartsIndex { get; init; } = reader.ReadByte();

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(UxActorId);
        writer.Write(IsDamageType);
        writer.Write(IsDamageTarget);
        writer.Write(Damage);
        writer.Write(Hp);
        writer.Write(ExtraMove);
        writer.Write(FlySpeed);
        writer.Write(AttackCounter);
        writer.Write(DefenseType);
        writer.Write(CurSuperArmorGauge);
        writer.Write(HitPartsIndex);
    }
}
