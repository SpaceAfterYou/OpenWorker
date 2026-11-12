using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct SkillActionValue
{
#region Message: Body

    public SkillActorInfoValue SkillActorInfo { get; init; }

    public int Skill { get; init; }

    public short TriggerIndex { get; init; }

    public ushort ContinuousHit { get; init; }

    public bool IsCounterAttack { get; init; }

    public bool IsPenetrate { get; init; }

#endregion Message: Body
}
