using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct SkillActorInfoValue
{
#region Message: Body

    public ActorValue UxActorId { get; init; }

    public SkillActionPosInfoValue SkillPosInfo { get; init; }

#endregion Message: Body
}
