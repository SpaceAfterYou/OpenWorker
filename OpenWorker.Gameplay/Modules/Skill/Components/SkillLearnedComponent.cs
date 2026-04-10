using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Gameplay.Modules.Skill.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct SkillLearnedComponent()
{
    public SkillInfoValue[] Values { get; init; } = new SkillInfoValue[byte.MaxValue];
}