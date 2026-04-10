using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

public readonly struct SkillDmgValue(BinaryReader reader) : IWritableData
{
#region Message: Body

    public ActorValue UxActorId { get; init; } = new(reader);

    public bool IsDamageType { get; init; } = reader.ReadBoolean();

    public bool IsDamageTarget { get; init; } = reader.ReadBoolean();

    public int Damage { get; init; } = reader.ReadInt32();

    public int Health { get; init; } = reader.ReadInt32();

    public Vector3 ExtraMove { get; init; } = reader.ReadVector3();

    public float FlySpeed { get; init; } = reader.ReadSingle();

    public byte AttackCounter { get; init; } = reader.ReadByte();

    public byte DefenseType { get; init; } = reader.ReadByte();

    public float CurSuperArmorGauge { get; init; } = reader.ReadSingle();

    public byte HitPartsIndex { get; init; } = reader.ReadByte();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(UxActorId);
        writer.Write(IsDamageType);
        writer.Write(IsDamageTarget);
        writer.Write(Damage);
        writer.Write(Health);
        writer.Write(ExtraMove);
        writer.Write(FlySpeed);
        writer.Write(AttackCounter);
        writer.Write(DefenseType);
        writer.Write(CurSuperArmorGauge);
        writer.Write(HitPartsIndex);
    }

#endregion Interface: IWritableData
}
