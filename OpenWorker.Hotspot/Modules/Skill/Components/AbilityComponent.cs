using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.Skill.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct AbilityComponent
{
    public required AbilityComponentEntry Health { get; init; }
    public required AbilityComponentEntry SoulGain { get; init; }
    public required AbilityComponentEntry SoulVapor { get; init; }
    public required AbilityComponentEntry Stamina { get; init; }
    public required AbilityComponentEntry SuperArmor { get; init; }

    public required SpeedComponentEntry Speed { get; init; }
}