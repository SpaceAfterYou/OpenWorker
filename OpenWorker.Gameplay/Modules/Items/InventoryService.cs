using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Res.Types;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// What happened to the slots of the inventory when something was added or taken away.
/// </summary>
public readonly record struct StorageChange(StorageGroup Storage, short Slot);

/// <summary>
/// The one place that changes the inventory of a person. Everything that gives or takes items goes through it
/// (pickup, rewards, shops, post, trade): it keeps the stacks right, tells the client and notes what to save.
/// </summary>
/// <remarks>Runs on the logic thread of the server, like everything that touches the world.</remarks>
public sealed class InventoryService(World world, ItemCatalog catalog, IItemSerials serials, PlayerStatsService? stats = null)
{
    /// <summary>The client has nothing more specific for a refused item request.</summary>
    public const ushort Refused = 52011;

    public ItemCatalog Catalog => catalog;

    public Inventory? InventoryOf(Entity player)
    {
        return world.IsAlive(player) && world.Has<InventoryComponent>(player) ? world.Get<InventoryComponent>(player).Value : null;
    }

    /// <summary>
    /// A new item as it comes out of the table: bound the way its type says, not yet in any slot.
    /// </summary>
    public ItemInstance Create(int item, int count)
    {
        catalog.TryGet(item, out var row);

        return new ItemInstance
        {
            Serial = serials.Next(),
            Item = item,
            Count = (short)count,
            BindType = (ItemBindType)row.Field8,
            Endurance = row.Field19
        };
    }

    #region Giving and taking

    /// <summary>
    /// How many of an item the person could still take, counting the free stack room and the empty slots of its storage.
    /// </summary>
    public long RoomFor(Entity player, int item)
    {
        var inventory = InventoryOf(player);
        var storage = inventory?.Find(catalog.HomeOf(item));

        if (storage is null)
        {
            return 0;
        }

        var limit = catalog.StackLimit(item);

        return storage.Slots.Sum(e => e is null ? limit : e.Item == item ? Math.Max(0, limit - e.Count) : 0L);
    }

    public bool CanGive(Entity player, int item, int count)
    {
        return count > 0 && catalog.Exists(item) && RoomFor(player, item) >= count;
    }

    /// <summary>
    /// Puts items into the inventory, all of them or none. The client is told which stacks grew and which slots are new,
    /// unless the caller has its own message for it (a purchase).
    /// </summary>
    public bool TryGive(Entity player, int item, int count, out List<StorageChange> changes, bool notify = true)
    {
        changes = [];

        if (!CanGive(player, item, count))
        {
            return false;
        }

        var inventory = InventoryOf(player)!;
        var storage = inventory.Find(catalog.HomeOf(item))!;
        var limit = catalog.StackLimit(item);
        var rest = count;

        var updated = new List<ItemInstance>();
        var created = new List<ItemInstance>();

        foreach (var stack in storage.Slots.OfType<ItemInstance>().Where(e => e.Item == item && e.Count < limit))
        {
            var add = Math.Min(rest, limit - stack.Count);

            stack.Count += (short)add;
            inventory.Touch(stack);
            updated.Add(stack);

            rest -= add;

            if (rest == 0)
            {
                break;
            }
        }

        while (rest > 0)
        {
            var add = Math.Min(rest, (int)limit);
            var instance = Create(item, add);

            inventory.Place(storage.Group, storage.FirstFree(), instance);
            created.Add(instance);

            rest -= add;
        }

        changes.AddRange(updated.Concat(created).Select(e => new StorageChange(e.Storage, e.Slot)));

        if (notify && world.Has<ServerSessionComponent>(player))
        {
            var session = world.Get<ServerSessionComponent>(player);

            if (updated.Count > 0)
            {
                session.Send(new ItemUpdateResponse { List = updated.Select(ToStorageValue).ToArray() });
            }

            if (created.Count > 0)
            {
                session.Send(new ItemCreateResponse { List = created.Select(ToStorageValue).ToArray() });
            }
        }

        return true;
    }

    public bool TryGive(Entity player, int item, int count)
    {
        return TryGive(player, item, count, out _);
    }

    /// <summary>
    /// How many of an item the person has outside the equipment.
    /// </summary>
    public long CountOf(Entity player, int item)
    {
        return InventoryOf(player)?.Storages
            .Where(e => !ItemCatalog.IsEquipment(e.Group))
            .SelectMany(e => e.Slots)
            .Where(e => e is not null && e.Item == item)
            .Sum(e => (long)e!.Count) ?? 0;
    }

    /// <summary>
    /// Takes items from the inventory, all of them or none. The smallest stacks go first.
    /// </summary>
    public bool TryTake(Entity player, int item, int count)
    {
        var inventory = InventoryOf(player);

        if (inventory is null || count <= 0 || CountOf(player, item) < count)
        {
            return false;
        }

        var stacks = inventory.Items
            .Where(e => e.Item == item && e.Storage is StorageGroup.Common or StorageGroup.Costume or StorageGroup.Cash or StorageGroup.Cube)
            .OrderBy(e => e.Count)
            .ToList();

        var rest = count;

        foreach (var stack in stacks)
        {
            if (rest == 0)
            {
                break;
            }

            var take = Math.Min(rest, (int)stack.Count);

            Reduce(player, inventory, stack, take);

            rest -= take;
        }

        return true;
    }

    /// <summary>
    /// Whether all of the items fit at once. An item that is about to be used up frees its slot.
    /// </summary>
    public bool CanGiveAll(Entity player, IReadOnlyCollection<(int Item, int Count)> items, ItemInstance? ignoring = null)
    {
        var inventory = InventoryOf(player);

        if (inventory is null)
        {
            return false;
        }

        var free = inventory.Storages.ToDictionary(e => e.Group, e => e.Slots.Count(x => x is null || (x == ignoring && x.Count == 1)));
        var room = new Dictionary<int, long>();

        foreach (var (item, count) in items)
        {
            if (!catalog.Exists(item) || count <= 0 || !free.ContainsKey(catalog.HomeOf(item)))
            {
                return false;
            }

            var home = catalog.HomeOf(item);
            var limit = (long)catalog.StackLimit(item);

            if (!room.TryGetValue(item, out var available))
            {
                available = inventory.Find(home)!.Slots.Where(e => e is not null && e.Item == item).Sum(e => Math.Max(0, limit - e!.Count));
            }

            var use = Math.Min(count, available);
            var rest = count - use;
            var slots = (rest + limit - 1) / limit;

            if (slots > free[home])
            {
                return false;
            }

            free[home] -= (int)slots;
            room[item] = available - use + slots * limit - rest;
        }

        return true;
    }

    /// <summary>
    /// Uses up some of an item the person holds.
    /// </summary>
    public void Spend(Entity player, ItemInstance item, int count)
    {
        if (InventoryOf(player) is { } inventory)
        {
            Reduce(player, inventory, item, count);
        }
    }

    /// <summary>
    /// Takes some of a stack away, or the stack itself when nothing is left.
    /// </summary>
    private void Reduce(Entity player, Inventory inventory, ItemInstance stack, int count)
    {
        var left = stack.Count - count;
        var group = stack.Storage;
        var slot = stack.Slot;

        if (left <= 0)
        {
            inventory.Destroy(group, slot);
            Send(player, new ItemBreakResponse { Storage = group, Index = slot });

            return;
        }

        stack.Count = (short)left;
        inventory.Touch(stack);
        Send(player, new ItemReduceResponse { Storage = group, Index = slot, Count = left });
    }

    #endregion

    #region Money

    public long Gold(Entity player) => world.Get<CurrencyComponent>(player).Gold;

    /// <summary>
    /// Adds or takes away gold. Taking more than the person has fails.
    /// </summary>
    public bool TryAddGold(Entity player, long amount, MoneyEarnFlow flow = MoneyEarnFlow.Normal)
    {
        return TryAddCurrency(player, ShopCurrency.Gold, amount, flow);
    }

    public bool TryAddCurrency(Entity player, ShopCurrency currency, long amount, MoneyEarnFlow flow = MoneyEarnFlow.Normal)
    {
        var wallet = world.Get<CurrencyComponent>(player);
        var total = wallet[currency] + amount;

        if (total < 0)
        {
            return false;
        }

        wallet[currency] = total;

        InventoryOf(player)?.MarkState();

        switch (currency)
        {
            case ShopCurrency.Gold:
            {
                Send(player, new ItemUpdateInvenMoneyResponse { Total = total, Bonus = 0, Type = flow });
                break;
            }

            case ShopCurrency.Bp:
            {
                Send(player, new ItemUpdateBpResponse { Actor = world.Get<ActorComponent>(player).Identifier, Total = total, Type = (byte)flow });
                break;
            }

            case ShopCurrency.Ether:
            {
                Send(player, new ItemUpdateEtherResponse { Total = total });
                break;
            }

            case ShopCurrency.Cash:
            {
                Send(player, new ItemUpdateCashResponse { Total = (int)total });
                break;
            }
        }

        return true;
    }

    #endregion

    #region Requests of the client

    public void Move(Entity player, MoveItemValue src, MoveItemValue dest)
    {
        var inventory = InventoryOf(player);

        if (inventory is null || !Resolve(inventory, src.Storage, src.Index, out var from) || !Resolve(inventory, dest.Storage, dest.Index, out var to))
        {
            Refuse(player, ItemOpcode.Move);
            return;
        }

        var item = inventory.At(src.Storage, src.Index);
        var other = inventory.At(dest.Storage, dest.Index);

        if (item is null || (src.Storage == dest.Storage && src.Index == dest.Index) || !CanStand(player, item, to, dest.Index) ||
            (other is not null && !CanStand(player, other, from, src.Index)))
        {
            Refuse(player, ItemOpcode.Move);
            return;
        }

        inventory.Lift(src.Storage, src.Index);
        inventory.Lift(dest.Storage, dest.Index);

        inventory.Place(dest.Storage, dest.Index, item);

        if (other is not null)
        {
            inventory.Place(src.Storage, src.Index, other);
        }

        // What is worn decides the stats.
        if (ItemCatalog.IsEquipment(src.Storage) || ItemCatalog.IsEquipment(dest.Storage))
        {
            stats?.Send(player);
        }

        Send(player, new ItemMoveResponse
        {
            Info =
            [
                new ItemMoveInfoValue
                {
                    Src = new MoveItemValue { Storage = src.Storage, Item = other?.Item ?? -1, Index = src.Index },
                    Dest = new MoveItemValue { Storage = dest.Storage, Item = item.Item, Index = dest.Index },
                    SrcBind = (byte)(other?.BindType ?? ItemBindType.None),
                    DestBind = (byte)item.BindType
                }
            ]
        });
    }

    public void Combine(Entity player, MoveItemValue src, MoveItemValue dest, short count)
    {
        var inventory = InventoryOf(player);
        var from = inventory?.At(src.Storage, src.Index);
        var to = inventory?.At(dest.Storage, dest.Index);

        if (inventory is null || from is null || to is null || from == to || count <= 0 || count > from.Count || from.Item != to.Item ||
            ItemCatalog.IsEquipment(src.Storage) || ItemCatalog.IsEquipment(dest.Storage) ||
            ItemCatalog.KindOf(src.Storage) != ItemCatalog.KindOf(dest.Storage) ||
            to.Count + count > catalog.StackLimit(to.Item))
        {
            Refuse(player, ItemOpcode.Combine);
            return;
        }

        var actor = world.Get<ActorComponent>(player);

        to.Count += count;
        from.Count -= count;

        inventory.Touch(to);

        // The client is sent the stack as it was left, an empty one is removed on its side.
        var left = from.ToValue();

        if (from.Count == 0)
        {
            inventory.Destroy(src.Storage, src.Index);
        }
        else
        {
            inventory.Touch(from);
        }

        Send(player, new ItemCombineResponse
        {
            SrcActor = actor,
            SrcStorage = src.Storage,
            SrcIndex = src.Index,
            SrcItem = left,
            DestActor = actor,
            DestStorage = dest.Storage,
            DestIndex = dest.Index,
            DestItem = to.ToValue()
        });
    }

    public void Divide(Entity player, MoveItemValue src, StorageGroup destStorage, short destIndex, short count)
    {
        var inventory = InventoryOf(player);
        var from = inventory?.At(src.Storage, src.Index);

        if (inventory is null || from is null || count <= 0 || count >= from.Count ||
            ItemCatalog.IsEquipment(src.Storage) || ItemCatalog.IsEquipment(destStorage) ||
            ItemCatalog.KindOf(src.Storage) != ItemCatalog.KindOf(destStorage) ||
            !Resolve(inventory, destStorage, destIndex, out _) || inventory.At(destStorage, destIndex) is not null)
        {
            Refuse(player, ItemOpcode.Divide);
            return;
        }

        var piece = Create(from.Item, count);

        piece.BindType = from.BindType;
        piece.Endurance = from.Endurance;

        from.Count -= count;

        inventory.Touch(from);
        inventory.Place(destStorage, destIndex, piece);

        Send(player, new ItemDivideResponse
        {
            SrcItem = from.Item,
            SrcStorage = src.Storage,
            SrcIndex = src.Index,
            SrcCount = from.Count,
            DestStorage = destStorage,
            DestIndex = destIndex,
            DestItem = piece.ToValue()
        });
    }

    /// <summary>
    /// Throws items away.
    /// </summary>
    public void Break(Entity player, StorageGroup group, short slot, long serial, int count)
    {
        var inventory = InventoryOf(player);
        var item = inventory?.At(group, slot);

        if (inventory is null || item is null || item.Serial != serial || count <= 0 || count > item.Count)
        {
            Refuse(player, ItemOpcode.Break);
            return;
        }

        Reduce(player, inventory, item, count);
    }

    /// <summary>
    /// Sorts the items of a storage to the front, the same items together.
    /// </summary>
    public void LineUp(Entity player, StorageGroup group)
    {
        var inventory = InventoryOf(player);
        var storage = inventory?.Find(group);

        if (inventory is null || storage is null || ItemCatalog.IsEquipment(group))
        {
            Refuse(player, ItemOpcode.LineUp);
            return;
        }

        var items = storage.Slots
            .OfType<ItemInstance>()
            .OrderBy(e => e.Item)
            .ThenByDescending(e => e.Count)
            .ThenBy(e => e.Serial)
            .ToList();

        foreach (var item in items)
        {
            inventory.Lift(group, item.Slot);
        }

        for (var index = 0; index < items.Count; index++)
        {
            inventory.Place(group, index, items[index]);
        }

        Send(player, new ItemLineUpResponse
        {
            Entries = items.Select(e => new ItemLineUpEntry(group, e.Slot, e.Serial, e.Count)).ToArray()
        });
    }

    /// <summary>
    /// Opens the next step of a storage if the person can pay for it.
    /// </summary>
    public void GradeUp(Entity player, StorageGroup group)
    {
        var inventory = InventoryOf(player);
        var storage = inventory?.Find(group);
        var step = catalog.Extension(group, (storage?.Grade ?? 0) + 1);

        if (inventory is null || storage is null || step is not { } info ||
            world.Get<RankComponent>(player).Level < info.NeedLevel)
        {
            Refuse(player, ItemOpcode.AddSlot);
            return;
        }

        switch (info.Payment)
        {
            case SlotExtendPaymentType.None:
            {
                break;
            }

            case SlotExtendPaymentType.Item:
            {
                if (!TryTake(player, info.Value, 1))
                {
                    Refuse(player, ItemOpcode.AddSlot);
                    return;
                }

                break;
            }

            default:
            {
                Refuse(player, ItemOpcode.AddSlot);
                return;
            }
        }

        var grade = (byte)(storage.Grade + 1);

        inventory.Resize(group, info.TotalSlots, grade);

        Send(player, new ItemAddSlotResponse { Storage = group, ExtendStep = grade, SlotCount = (short)info.TotalSlots });
    }

    #endregion

    #region Packets

    /// <summary>
    /// The items of the storages the client asks for, as the answer to the request for them.
    /// </summary>
    public StorageItemValue[] ListItems(Entity player, IEnumerable<StorageGroup> groups)
    {
        var inventory = InventoryOf(player);

        if (inventory is null)
        {
            return [];
        }

        return groups
            .Distinct()
            .Select(inventory.Find)
            .OfType<ItemStorage>()
            .SelectMany(e => e.Slots.OfType<ItemInstance>())
            .Select(ToStorageValue)
            .ToArray();
    }

    public ItemOpenSlotInfoResponse DescribeSlots(Entity player, IEnumerable<StorageGroup> groups)
    {
        var inventory = InventoryOf(player);

        return new ItemOpenSlotInfoResponse
        {
            Groups = groups
                .Distinct()
                .Select(e => inventory?.Find(e))
                .OfType<ItemStorage>()
                .Select(e => new ItemOpenSlotGroupEntry(e.Group, (short)e.Size, e.Grade))
                .ToList()
        };
    }

    public static StorageItemValue ToStorageValue(ItemInstance item)
    {
        return new StorageItemValue(item.Storage, item.Slot, item.ToValue());
    }

    #endregion

    #region Helpers

    private void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }

    private void Refuse(Entity player, ItemOpcode request, ushort code = Refused)
    {
        Send(player, new ErrorMessageResponse(new MessageOpcode(GroupOpcode.Item, request), code));
    }

    private static bool Resolve(Inventory inventory, StorageGroup group, int slot, out ItemStorage storage)
    {
        storage = inventory.Find(group)!;

        return storage is not null && slot >= 0 && slot < storage.Size;
    }

    /// <summary>
    /// Whether the item may lie in the slot: a clothes slot takes only what fits it, a bag only items of its kind.
    /// </summary>
    private bool CanStand(Entity player, ItemInstance item, ItemStorage storage, int slot)
    {
        if (!ItemCatalog.IsEquipment(storage.Group))
        {
            return ItemCatalog.KindOf(storage.Group) == catalog.HomeOf(item.Item);
        }

        if (catalog.EquipPlaceOf(item.Item) is not { } place || place.Storage != storage.Group || !place.Slots.Contains((short)slot))
        {
            return false;
        }

        var (level, hero) = catalog.Limits(item.Item);

        return world.Get<RankComponent>(player).Level >= level &&
               (hero == 0 || hero == (byte)world.Get<PersonInfoComponent>(player).Hero);
    }

    #endregion
}
