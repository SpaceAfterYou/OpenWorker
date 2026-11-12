using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Types;

[WireMessage]
public readonly partial struct SkillAkashicRecordValue
{
#region Message: Body

    public uint Akashic { get; init; }
    public ActorValue Player { get; init; }
    public SkillPositionValue SkillPosition { get; init; }

#endregion Message: Body
}
