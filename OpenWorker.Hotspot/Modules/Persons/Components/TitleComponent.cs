using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.Persons.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct TitleComponent
{
    public required int Primary { get; init; }
    public required int Secondary { get; init; }
}