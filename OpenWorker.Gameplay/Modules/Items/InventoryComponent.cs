using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// The inventory of the person a connection plays.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct InventoryComponent(Inventory? Value);
