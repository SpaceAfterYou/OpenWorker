using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct SkillDmgValue
{
#region Message: Body

    public ActorValue UxActorId { get; init; }

    public byte ReactionType { get; init; }

    public byte DamageFlags { get; init; }

    public int Damage { get; init; }

    public int Health { get; init; }

    public Vector3 ExtraMove { get; init; }

    public float FlySpeed { get; init; }

    public byte AttackCounter { get; init; }

    public byte DefenseType { get; init; }

    public float CurSuperArmorGauge { get; init; }

    public byte HitPartsIndex { get; init; }

#endregion Message: Body
}
