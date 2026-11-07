using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Persons.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct TitleComponent
{
    public int AbilityPrimary { get; init; }
    public int AbilitySecondary { get; init; }
    public required int Primary { get; init; }
    public required int Secondary { get; init; }
}