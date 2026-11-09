using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Items;

namespace OpenWorker.Gameplay.Modules.Shop.Components;

/// <summary>
/// The items the person sold in this city that a shop would sell back, oldest first. Kept while the person stays.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct ShopRepurchaseComponent
{
    public List<ItemInstance>? Items;
}
