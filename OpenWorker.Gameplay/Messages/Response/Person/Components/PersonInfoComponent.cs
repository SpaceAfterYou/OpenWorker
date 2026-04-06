using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Messages.Response.Person.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct PersonInfoComponent
{
    public required string Name { get; init; }
    public required Hero Hero { get; init; }
}