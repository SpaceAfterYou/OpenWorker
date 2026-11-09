using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Handler.Attributes;

namespace OpenWorker.Server.Common.Services;

public sealed class ItemService(World world, InventoryService inventory, ItemUseService use) : ItemServiceBase(world, inventory, use);
