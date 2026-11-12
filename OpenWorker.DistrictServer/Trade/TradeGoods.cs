using System.Globalization;
using System.Text;
using Arch.Core;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.DistrictServer.Trade;

/// <summary>
/// How items and gold go from one player to another, for a trade and for a private shop: what may be offered, whether
/// it is still there, whether it fits, and the move itself. Everything here is synchronous and runs on the world loop,
/// so a check and the move that follows it see the same bags.
/// </summary>
public sealed class TradeGoods(World world, InventoryService inventory)
{
    public InventoryService Inventory => inventory;

    /// <summary>
    /// Only what lies in the bags can be traded: not what is worn, nor what is in a storage.
    /// </summary>
    public static bool IsTradeBag(StorageGroup group)
    {
        return group is StorageGroup.Common or StorageGroup.Costume or StorageGroup.Cash;
    }

    /// <summary>
    /// The item in the slot, if it is one the player may hand over; otherwise the code to answer with.
    /// </summary>
    public ushort Offerable(Entity player, StorageGroup group, short slot, out ItemInstance item)
    {
        item = null!;

        if (!IsTradeBag(group) || inventory.InventoryOf(player)?.At(group, slot) is not { } found)
        {
            return TradeCodes.ItemRefused;
        }

        // A broken item is asked before a bound one.
        switch (ItemBinding.HeldBack(found))
        {
            case ItemHeldBack.Broken:
                return TradeCodes.ItemRefused;

            case ItemHeldBack.Bound:
                return TradeCodes.ItemBound;
        }

        item = found;

        return 0;
    }

    public static TradeOffer Describe(ItemInstance item)
    {
        return new TradeOffer(item.Storage, item.Slot, item.Serial, item.Item, item.Count, LookOf(item));
    }

    /// <summary>
    /// Everything of an item that the workshops can change while it lies on the table: its upgrade, whether it is broken
    /// or bound, its wear, its sockets, options and broaches. Two items with the same text are the same to a buyer.
    /// </summary>
    private static string LookOf(ItemInstance item)
    {
        var text = new StringBuilder();

        text.Append(CultureInfo.InvariantCulture,
            $"{(int)item.BindState}|{item.Upgrade}|{item.Endurance}|{item.SocketActive}|{item.UpgradeCount}|{item.UpgradeLimit}|{item.Flag}|{item.Exp}|{item.BroachState}|{item.RestoreCount}|{item.SealCount}");

        foreach (var option in item.Options)
        {
            text.Append(CultureInfo.InvariantCulture, $"|o{option.Type}:{option.Option}");
        }

        foreach (var socket in item.Sockets)
        {
            text.Append(CultureInfo.InvariantCulture, $"|s{socket.Gem}");

            foreach (var option in socket.Options ?? [])
            {
                text.Append(CultureInfo.InvariantCulture, $",{option.Type}:{option.Option}");
            }
        }

        foreach (var broach in item.Broaches)
        {
            text.Append(CultureInfo.InvariantCulture, $"|b{broach}");
        }

        return text.ToString();
    }

    /// <summary>
    /// Whether the item offered is still where it was and still the same: not moved, split, used or sold since, and
    /// not upgraded, broken, bound or fitted with anything at a workshop while it lay on the table.
    /// </summary>
    public bool StillHolds(Entity player, TradeOffer offer)
    {
        return inventory.InventoryOf(player)?.At(offer.Storage, offer.Slot) is { } item &&
               item.Serial == offer.Serial &&
               item.Item == offer.Item &&
               item.Count == offer.Count &&
               ItemBinding.HeldBack(item) == ItemHeldBack.Not &&
               LookOf(item) == offer.Look;
    }

    /// <summary>
    /// Whether the items coming in fit the bags of the player once the items it hands over in the same exchange are out.
    /// Every item takes a slot of its own.
    /// </summary>
    public bool HasRoom(Entity player, IReadOnlyList<TradeOffer> incoming, IReadOnlyList<TradeOffer> outgoing)
    {
        if (inventory.InventoryOf(player) is not { } bags)
        {
            return false;
        }

        var free = new Dictionary<StorageGroup, int>();

        foreach (var storage in bags.Storages)
        {
            free[storage.Group] = storage.Slots.Count(e => e is null);
        }

        foreach (var offer in outgoing)
        {
            free[offer.Storage] = free.GetValueOrDefault(offer.Storage) + 1;
        }

        foreach (var offer in incoming)
        {
            var home = inventory.Catalog.HomeOf(offer.Item);

            if (!free.TryGetValue(home, out var left) || left == 0)
            {
                return false;
            }

            free[home] = left - 1;
        }

        return true;
    }

    /// <summary>
    /// Whether the player can take the gold without its purse running over (the limit of the inventory).
    /// </summary>
    public bool CanReceive(Entity player, long amount)
    {
        return inventory.CanReceive(player, ShopCurrency.Gold, amount);
    }

    /// <summary>
    /// Takes the offered items out of the bags of the player, for good, and tells its client. The caller has checked them
    /// with <see cref="StillHolds"/>.
    /// </summary>
    public List<ItemInstance> Take(Entity player, IReadOnlyList<TradeOffer> offers)
    {
        var bags = inventory.InventoryOf(player)!;
        var taken = new List<ItemInstance>(offers.Count);

        foreach (var offer in offers)
        {
            taken.Add(bags.At(offer.Storage, offer.Slot)!);

            // The row of the item goes with its owner: the item comes back to life in the new bags under a new serial.
            bags.Destroy(offer.Storage, offer.Slot);

            Send(player, new ItemBreakResponse { Storage = offer.Storage, Index = offer.Slot });
        }

        return taken;
    }

    /// <summary>
    /// Puts copies of the items into free slots of the player and tells its client. The caller has checked the room with
    /// <see cref="HasRoom"/>.
    /// </summary>
    public void Give(Entity player, List<ItemInstance> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        var bags = inventory.InventoryOf(player)!;
        var created = new StorageItemValue[items.Count];

        for (var index = 0; index < items.Count; index++)
        {
            var copy = CopyOf(items[index]);
            var storage = bags.Find(inventory.Catalog.HomeOf(copy.Item))!;

            bags.Place(storage.Group, storage.FirstFree(), copy);

            created[index] = InventoryService.ToStorageValue(copy);
        }

        Send(player, new ItemCreateResponse { List = created });
    }

    /// <summary>
    /// The same item under a new serial: a serial belongs to the row of one person in the database, and the write of the
    /// item does not move a row from one person to another.
    /// </summary>
    private ItemInstance CopyOf(ItemInstance item)
    {
        var copy = inventory.Create(item.Item, item.Count);

        copy.BindState = item.BindState;
        copy.Upgrade = item.Upgrade;
        copy.Endurance = item.Endurance;
        copy.SocketActive = item.SocketActive;
        copy.DueTo = item.DueTo;
        copy.UpgradeCount = item.UpgradeCount;
        copy.UpgradeLimit = item.UpgradeLimit;
        copy.Flag = item.Flag;
        copy.Exp = item.Exp;
        copy.BroachState = item.BroachState;
        copy.RestoreCount = item.RestoreCount;
        copy.SealCount = item.SealCount;
        copy.Options = (ItemOption[])item.Options.Clone();
        copy.Sockets = (ItemSocket[])item.Sockets.Clone();
        copy.Broaches = (int[])item.Broaches.Clone();

        return copy;
    }

    private void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }
}
