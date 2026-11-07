using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Gameplay.Modules.Persons.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct CommunityComponent
{
    public required CommunityState State { get; init; }
    public required string Comment { get; init; }
    public required string Memo { get; init; }
}
