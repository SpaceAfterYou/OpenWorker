using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Shop.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct ShopPrivateComponent
{
    public required byte Type { get; init; }
    public required string Name { get; init; }
}