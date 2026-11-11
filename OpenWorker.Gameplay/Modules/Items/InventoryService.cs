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
using OpenWorker.Gameplay.Modules.Persistence;

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
public sealed class InventoryService(
    World world,
    ItemCatalog catalog,
    IItemSerials serials,
    PlayerStatsService? stats = null,
    CostumeBroachPlaces? broaches = null,
    ItemArrivals? arrivals = null,
    ItemOptionRoller? options = null)
{
    /// <summary>
    /// The most gold, battle points, ether and recycle points a person can hold: fourteen digits. A gift that would take a
    /// purse past it is refused as a whole (a reward, a sale, a trade, a letter), except for what is picked up from the ground,
    /// which fills the purse and leaves the rest.
    /// </summary>
    public const long WalletLimit = 99_999_999_999_999;

    /// <summary>
    /// The most cash and cash points a person can hold: nine digits.
    /// </summary>
    public const long CashLimit = 999_999_999;

    /// <summary>The gold of the person would pass <see cref="WalletLimit"/>: the code of a reward or a sale that is refused for it.</summary>
    public const ushort TooMuchGold = 52056;

    /// <summary>The same for battle points.</summary>
    public const ushort TooMuchBattlePoints = 52057;

    /// <summary>The same for ether.</summary>
    public const ushort TooMuchEther = 52058;

    /// <summary>The client has nothing more specific for a refused item request.</summary>
    public const ushort Refused = 52011;

    /// <summary>The item may not be put into the bank, or not straight from the equipment.</summary>
    public const ushort CannotStore = 52044;

    /// <summary>The storage has no step to open, the person is below its level, or the game takes no such payment for it.</summary>
    public const ushort NoStep = 52023;

    /// <summary>The person has not the gold a step of a storage costs.</summary>
    public const ushort NotEnoughGold = 52301;

    public ItemCatalog Catalog => catalog;

    public Inventory? InventoryOf(Entity player)
    {
        return world.IsAlive(player) && world.Has<InventoryComponent>(player) ? world.Get<InventoryComponent>(player).Value : null;
    }

    /// <summary>
    /// A new item as it comes out of the table: bound if its rule says it binds from the start (<see cref="ItemBinding"/>), with
    /// the places for broaches of its kind, its options rolled and the sockets it opens with, not yet in any slot.
    /// </summary>
    public ItemInstance Create(int item, int count)
    {
        catalog.TryGet(item, out var row);

        var instance = new ItemInstance
        {
            Serial = serials.NextSerial(),
            Item = item,
            Count = (short)count,
            BindState = ItemBinding.OnMade((ItemBindType)row.BindType),
            Endurance = row.Endurance,
            BroachState = broaches?.StateOf(item) ?? ItemInstance.NoBroaches
        };

        options?.Roll(instance, row, catalog.Classify(item));

        return instance;
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

        // New items join the stacks that are bound as they are (a free stack never takes bound items, nor the other way round).
        var rule = catalog.TryGet(item, out var row) ? (ItemBindType)row.BindType : ItemBindType.None;
        var made = ItemBinding.OnMade(rule);

        foreach (var stack in storage.Slots.OfType<ItemInstance>().Where(e => e.Item == item && e.Count < limit && ItemBinding.CanStack(e.BindState, made, rule)))
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

        arrivals?.Arrived(player, item, count);

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

    #region What helpers wear

    /// <summary>
    /// The item a helper of the person wears at the place (<see cref="Inventory.HelperPlace"/>), if any.
    /// </summary>
    public ItemInstance? WornByHelper(Entity player, short place)
    {
        return InventoryOf(player)?.WornByHelper(place);
    }

    /// <summary>
    /// Puts an item of a bag on a helper. The helper's place must be free unless <paramref name="swap"/> is set: then the
    /// item worn there goes to the bag slot the new item leaves. The client is told that the item left its slot and, on a
    /// swap, that the other one came into it.
    /// </summary>
    public bool PutOnHelper(Entity player, ItemInstance item, short place, bool swap)
    {
        var inventory = InventoryOf(player);
        var group = item.Storage;
        var slot = item.Slot;

        if (inventory is null || place < 0 || !ReferenceEquals(inventory.At(group, slot), item))
        {
            return false;
        }

        var worn = inventory.WornByHelper(place);

        if ((worn is not null) != swap)
        {
            return false;
        }

        inventory.Lift(group, slot);
        Send(player, new ItemBreakResponse { Storage = group, Index = slot });

        if (worn is not null)
        {
            inventory.TakeOffHelper(place);
        }

        inventory.PutOnHelper(place, item);

        if (worn is not null)
        {
            inventory.Place(group, slot, worn);
            Send(player, new ItemCreateResponse { List = [ToStorageValue(worn)] });
        }

        return true;
    }

    /// <summary>
    /// Takes the item a helper wears at the place off it, into a free slot of the bag of its kind: the slot the client named
    /// if that one is free, else the first free one. False, and nothing moves, when the bag is full.
    /// </summary>
    public bool TakeOffHelper(Entity player, short place, StorageGroup group, short slot)
    {
        var inventory = InventoryOf(player);

        if (inventory?.WornByHelper(place) is not { } worn || inventory.Find(catalog.HomeOf(worn.Item)) is not { } bag)
        {
            return false;
        }

        if (group != bag.Group || slot < 0 || slot >= bag.Size || bag.Slots[slot] is not null)
        {
            slot = (short)bag.FirstFree();
        }

        if (slot < 0)
        {
            return false;
        }

        inventory.TakeOffHelper(place);
        inventory.Place(bag.Group, slot, worn);
        Send(player, new ItemCreateResponse { List = [ToStorageValue(worn)] });

        return true;
    }

    #endregion

    #region Money

    public long Gold(Entity player) => world.Get<CurrencyComponent>(player).Gold;

    /// <summary>
    /// How much of a currency the person has.
    /// </summary>
    public long Balance(Entity player, ShopCurrency currency) => world.Get<CurrencyComponent>(player)[currency];

    /// <summary>
    /// Adds or takes away gold. Taking more than the person has fails, and so does adding more than its purse holds.
    /// </summary>
    public bool TryAddGold(Entity player, long amount, MoneyEarnFlow flow = MoneyEarnFlow.Normal)
    {
        return TryAddCurrency(player, ShopCurrency.Gold, amount, flow);
    }

    /// <summary>
    /// The most of a currency a person can hold.
    /// </summary>
    public static long LimitOf(ShopCurrency currency)
    {
        return currency switch
        {
            ShopCurrency.Gold or ShopCurrency.Bp or ShopCurrency.Ether or ShopCurrency.Recycle => WalletLimit,
            ShopCurrency.Cash or ShopCurrency.CashMileageAkashic or ShopCurrency.CashMileageBroach or ShopCurrency.CashMileageTag => CashLimit,
            _ => long.MaxValue
        };
    }

    /// <summary>
    /// Whether the person can take the amount without its purse of the currency passing the limit. Taking something away
    /// always fits.
    /// </summary>
    public bool CanReceive(Entity player, ShopCurrency currency, long amount)
    {
        return amount <= 0 || Balance(player, currency) <= LimitOf(currency) - amount;
    }

    /// <summary>
    /// Adds what fits of the amount under the limit of the currency and tells how much that was: the way what lies on the ground
    /// is picked up.
    /// </summary>
    public long AddUpToLimit(Entity player, ShopCurrency currency, long amount, MoneyEarnFlow flow = MoneyEarnFlow.Normal)
    {
        var room = Math.Max(0, LimitOf(currency) - Balance(player, currency));
        var added = Math.Clamp(amount, 0, room);

        return added == 0 || TryAddCurrency(player, currency, added, flow) ? added : 0;
    }

    /// <summary>
    /// Adds or takes away an amount of a currency. Taking more than the person has fails, and so does adding what would take
    /// the purse past its limit.
    /// </summary>
    public bool TryAddCurrency(Entity player, ShopCurrency currency, long amount, MoneyEarnFlow flow = MoneyEarnFlow.Normal)
    {
        var wallet = world.Get<CurrencyComponent>(player);
        var total = wallet[currency] + amount;

        if (total < 0 || amount > 0 && !CanReceive(player, currency, amount))
        {
            return false;
        }

        wallet[currency] = total;

        world.MarkDirty(player, PersonSections.Currencies);

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

                if (amount > 0)
                {
                    arrivals?.BattlePointsArrived(player, amount);
                }

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

        if (!MayStore(item, from, to) || (other is not null && !MayStore(other, to, from)))
        {
            Refuse(player, ItemOpcode.Move, CannotStore);
            return;
        }

        // An item that goes on binds when its rule says so, and so does the one that takes its place on the other side.
        BindWhenWorn(item, src.Storage, dest.Storage);

        if (other is not null)
        {
            BindWhenWorn(other, dest.Storage, src.Storage);
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
                    SrcBind = (byte)(other?.BindState ?? ItemBindState.Free),
                    DestBind = (byte)item.BindState
                }
            ]
        });
    }

    private void BindWhenWorn(ItemInstance item, StorageGroup from, StorageGroup to)
    {
        if (ItemCatalog.IsEquipment(to) && !ItemCatalog.IsEquipment(from) && catalog.TryGet(item.Item, out var row))
        {
            item.BindState = ItemBinding.OnWorn(item.BindState, (ItemBindType)row.BindType);
        }
    }

    public void Combine(Entity player, MoveItemValue src, MoveItemValue dest, short count)
    {
        var inventory = InventoryOf(player);
        var from = inventory?.At(src.Storage, src.Index);
        var to = inventory?.At(dest.Storage, dest.Index);

        // Stacks pour into one another only when both are bound alike, or a bound stack would come out free.
        if (inventory is null || from is null || to is null || from == to || count <= 0 || count > from.Count || from.Item != to.Item ||
            from.BindState != to.BindState ||
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

        piece.BindState = from.BindState;
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
    /// Opens the next step of a storage if the person can pay for it: with gold, or with the item its table names (a bag or a
    /// bank step). A step the storage has not, a level that is too low and a payment the game does not take are refused with
    /// <see cref="NoStep"/>, too little gold with <see cref="NotEnoughGold"/>.
    /// </summary>
    public void GradeUp(Entity player, StorageGroup group)
    {
        var inventory = InventoryOf(player);
        var storage = inventory?.Find(group);
        var step = catalog.Extension(group, (storage?.Grade ?? 0) + 1);

        if (inventory is null || storage is null || step is not { } info ||
            world.Get<LevelComponent>(player).Level < info.NeedLevel)
        {
            Refuse(player, ItemOpcode.AddSlot, NoStep);
            return;
        }

        switch (info.Payment)
        {
            case SlotExtendPaymentType.None:
            {
                break;
            }

            case SlotExtendPaymentType.Gold:
            {
                if (world.Get<CurrencyComponent>(player).Gold < info.Value)
                {
                    Refuse(player, ItemOpcode.AddSlot, NotEnoughGold);
                    return;
                }

                TryAddGold(player, -info.Value);

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
                Refuse(player, ItemOpcode.AddSlot, NoStep);
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

    /// <summary>
    /// The gems in the sockets of every item of the person, as the client loads them with the bags.
    /// </summary>
    public ST_SOCKET_DATA[] ListSockets(Entity player)
    {
        return InventoryOf(player)?.Items.SelectMany(e => e.SocketValues()).ToArray() ?? [];
    }

    /// <summary>
    /// The broaches set into every item of the person, as the client loads them with the bags.
    /// </summary>
    public ST_ITEM_BROACH[] ListBroaches(Entity player)
    {
        return InventoryOf(player)?.Items.Select(e => e.BroachValue()).OfType<ST_ITEM_BROACH>().ToArray() ?? [];
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
        Send(player, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Item, request), code));
    }

    private static bool Resolve(Inventory inventory, StorageGroup group, int slot, out ItemStorage storage)
    {
        storage = inventory.Find(group)!;

        return storage is not null && slot >= 0 && slot < storage.Size;
    }

    /// <summary>
    /// Whether an item may go from one storage to the other as far as the bank goes: a worn item goes through the bags, and
    /// items whose row keeps them out of storage stay out.
    /// </summary>
    private bool MayStore(ItemInstance item, ItemStorage from, ItemStorage to)
    {
        if (to.Group is not (StorageGroup.CommonStorage or StorageGroup.CostumeStorage or StorageGroup.CashStorage))
        {
            return true;
        }

        return !ItemCatalog.IsEquipment(from.Group) && (!catalog.TryGet(item.Item, out var row) || row.SellLimit is not (4 or 5 or 6 or 7));
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

        return world.Get<LevelComponent>(player).Level >= level &&
               (hero == 0 || hero == (byte)world.Get<PersonInfoComponent>(player).Hero);
    }

    #endregion
}
