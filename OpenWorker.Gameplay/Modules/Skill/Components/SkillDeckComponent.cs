using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Gameplay.Modules.Skill.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct SkillDeckComponent
{
    public required SkillDeckValue[] SlotList { get; init; }
    public required short SlotCount { get; init; }
}