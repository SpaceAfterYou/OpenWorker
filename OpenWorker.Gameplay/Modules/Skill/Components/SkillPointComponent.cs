using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Skill.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct SkillPointComponent
{
    public required short TotalSkillPoint { get; init; }
    public required short FreeSkillPoint { get; init; }
}