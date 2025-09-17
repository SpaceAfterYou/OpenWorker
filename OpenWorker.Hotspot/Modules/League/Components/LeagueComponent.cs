using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.League.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct LeagueComponent
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required LeagueComponentCard Card { get; init; }
}