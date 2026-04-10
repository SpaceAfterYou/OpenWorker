using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Skill.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct SkillDeckBonusComponent
{
    public required ushort[] Values { get; init; }
}