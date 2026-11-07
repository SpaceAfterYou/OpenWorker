using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class ItemService(World world, InventoryService inventory, ItemUseService use) : ItemServiceBase(world, inventory, use);
