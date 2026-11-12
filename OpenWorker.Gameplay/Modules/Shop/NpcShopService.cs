using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.Shop.Responses;
using OpenWorker.Hotspot.Modules.Shop.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Shop;

/// <summary>
/// The shops of the NPCs of a city: buying a line of the shop of an NPC for the currency of the line, selling items of the
/// bags for their price, and buying sold items back. Every request is answered, refused or not, so the shop window never
/// waits; a refusal changes nothing.
/// </summary>
/// <remarks>Runs on the world loop. Items change through <see cref="InventoryService"/>, so the flush writes them.</remarks>
public sealed class NpcShopService(
    World world,
    InventoryService inventory,
    ServiceNpcs npcs,
    NpcCreditManager credit,
    ReadOnlyCollection<ShopRow> lines,
    QuestManager? quests = null,
    AchievementManager? achievements = null)
{
    /// <summary>Gold, battle points or ether short of the price; gold short of buying back.</summary>
    public const ushort NotEnoughMoney = 52301;

    /// <summary>The line asks for a currency the shop does not take, or sells below what the item sells for.</summary>
    public const ushort WrongPrice = 52302;

    /// <summary>The NPC is not there or too far, or the line sells nothing.</summary>
    public const ushort CannotBuy = 52305;

    /// <summary>A count of none or more than a stack.</summary>
    public const ushort WrongCount = 52306;

    /// <summary>The item is not among those the shop would sell back.</summary>
    public const ushort NotRepurchasable = 52307;

    /// <summary>The NPC of a buying back keeps no shop, or stands too far.</summary>
    public const ushort NoShop = 52308;

    /// <summary>The line is not of the shop of the NPC, or asks for more credit with its group.</summary>
    public const ushort NotForSale = 52309;

    /// <summary>The bags have no room for what is bought.</summary>
    public const ushort NoRoom = 52311;

    /// <summary>An item that cannot be sold, a slot that holds nothing, more items than a sale takes.</summary>
    public const ushort CannotSell = 52312;

    /// <summary>The tokens the line asks for are short.</summary>
    public const ushort NotEnoughTokens = 52316;

    /// <summary>The token a line of the token currency is paid with.</summary>
    public const int Token = 800000999;

    /// <summary>The most items one sale takes.</summary>
    public const int MaxSold = 10;

    /// <summary>The most items a shop would sell back; the oldest goes first.</summary>
    public const int MaxRepurchase = 12;

    /// <summary>The currency of a line that pays with tokens, items of the bags.</summary>
    private const byte TokenPrice = 4;

    private readonly RowIndex<int, ShopRow> _lines = lines.IndexBy(e => e.Id);

    /// <summary>
    /// Buys <paramref name="count"/> of the item of a line of the shop of the NPC: the price of the line, or the price of the
    /// item when the line names none, in the currency of the line.
    /// </summary>
    public void Buy(Entity player, ActorValue npcActor, int index, short count)
    {
        var request = Opcode(ShopOpcode.Buy);

        if (!npcs.TryServe(player, npcActor, NpcFunction.Shop, request, CannotBuy, out var npc))
        {
            return;
        }

        // A line is sold by the shop of its group, and from the grade of credit it asks for.
        if (!_lines.TryGet(index, out var line) || line.Group != npc.Function ||
            (line.CreditGrade > 0 && credit.GradeOf(player, npc.Group) < line.CreditGrade))
        {
            Refuse(player, request, NotForSale);
            return;
        }

        if (line.Item == 0 || !inventory.Catalog.TryGet(line.Item, out var item))
        {
            Refuse(player, request, CannotBuy);
            return;
        }

        if (count <= 0 || count > Math.Max((short)1, item.Stack))
        {
            Refuse(player, request, WrongCount);
            return;
        }

        var price = (long)count * (line.Price != 0 ? line.Price : item.Price);

        if (Shortfall(player, line.PriceType, price, item, count) is { } refusal)
        {
            Refuse(player, request, refusal);
            return;
        }

        // The goods are given before the price is paid: a refused gift takes nothing.
        if (!inventory.TryGive(player, line.Item, count, out var changes))
        {
            Refuse(player, request, NoRoom);
            return;
        }

        Pay(player, line.PriceType, price);

        var bag = inventory.InventoryOf(player)!;

        Send(player, new ShopBuyResponse
        {
            StorageList = changes
                .Select(e => bag.At(e.Storage, e.Slot))
                .OfType<ItemInstance>()
                .Select(e => new StorageValue { Storage = e.Storage, Slot = e.Slot, Item = e.ToValue() })
                .ToArray(),
            Spent = price,
            Currency = (ShopCurrency)line.PriceType
        });

        // A purchase counts once for the quests that ask to buy the item, and raises the credit with the shop's group.
        quests?.OnEvent(player, QuestConditionType.Buy, QuestConditionTarget.Item, line.Item);
        achievements?.Count(player, AchievementType.ShopPurchase);
        credit.Raise(player, npc, CreditRaise.Buy);
    }

    /// <summary>
    /// Sells whole stacks of the bags to the shop of an NPC: an item for its sell price times its count, a broken item for one
    /// gold. Each item sold goes to what the shop would sell back.
    /// </summary>
    public void Sell(Entity player, ActorValue npcActor, IReadOnlyList<ShopSellEntry> entries)
    {
        var request = Opcode(ShopOpcode.Sell);

        if (!npcs.TryServe(player, npcActor, NpcFunction.None, request, CannotSell, out _))
        {
            return;
        }

        if (inventory.InventoryOf(player) is not { } bag || entries.Count is 0 or > MaxSold)
        {
            Refuse(player, request, CannotSell);
            return;
        }

        var sold = new List<ItemInstance>(entries.Count);
        var money = 0L;

        foreach (var entry in entries)
        {
            if (entry.Storage is not (StorageGroup.Common or StorageGroup.Costume or StorageGroup.Cash) ||
                bag.At(entry.Storage, entry.Slot) is not { Count: > 0 } item || sold.Contains(item) ||
                !inventory.Catalog.TryGet(item.Item, out var row) || !IsSellable(row))
            {
                Refuse(player, request, CannotSell);
                return;
            }

            sold.Add(item);
            money += item.Flag == ItemRestoreService.BrokenFlag ? 1 : (long)item.Count * row.SellPrice;
        }

        // A sale the purse would not hold is refused as a whole, with the code of the gold that is too much.
        if (!inventory.CanReceive(player, ShopCurrency.Gold, money))
        {
            Refuse(player, request, InventoryService.TooMuchGold);
            return;
        }

        var values = sold.Select(InventoryService.ToStorageValue).ToArray();

        foreach (var item in sold)
        {
            bag.Destroy(item.Storage, item.Slot);
        }

        if (money > 0)
        {
            inventory.TryAddGold(player, money);
        }

        Send(player, new ShopSellResponse { Items = values });

        var kept = Repurchasable(player);

        kept.AddRange(sold);

        if (kept.Count > MaxRepurchase)
        {
            kept.RemoveRange(0, kept.Count - MaxRepurchase);
        }

        Send(player, new ShopRepurchaserAddResponse { Items = sold.Select(e => e.ToValue()).ToArray() });

        foreach (var item in sold)
        {
            quests?.OnEvent(player, QuestConditionType.Sell, QuestConditionTarget.Item, item.Item);
        }
    }

    /// <summary>
    /// The list of what a shop would sell back, asked when the shop window opens at an NPC that keeps a shop.
    /// </summary>
    public void RepurchaseList(Entity player, ActorValue npcActor)
    {
        if (!npcs.TryServe(player, npcActor, NpcFunction.Shop, Opcode(ShopOpcode.RepurchaserList), NoShop, out _))
        {
            return;
        }

        Send(player, new ShopRepurchaserListResponse { Items = Repurchasable(player).Select(e => e.ToValue()).ToArray() });
    }

    /// <summary>
    /// Buys back an item sold in this city for what it was sold for, one gold at the least. It comes back as it was, under
    /// a new serial.
    /// </summary>
    public void Repurchase(Entity player, ActorValue npcActor, ItemValue wanted)
    {
        var request = Opcode(ShopOpcode.Repurchaser);

        if (!npcs.TryServe(player, npcActor, NpcFunction.Shop, request, NoShop, out _))
        {
            return;
        }

        var kept = Repurchasable(player);
        var item = kept.Find(e => e.Serial == wanted.Serial.Number && e.Item == wanted.Item && e.Count == wanted.Count);

        if (item is null || inventory.InventoryOf(player) is not { } bag || !inventory.Catalog.TryGet(item.Item, out var row) ||
            bag.Find(inventory.Catalog.HomeOf(item.Item)) is not { } home)
        {
            Refuse(player, request, NotRepurchasable);
            return;
        }

        var price = Math.Max(1L, (long)item.Count * row.SellPrice);

        if (inventory.Gold(player) < price)
        {
            Refuse(player, request, NotEnoughMoney);
            return;
        }

        var slot = home.FirstFree();

        if (slot < 0)
        {
            Refuse(player, request, NoRoom);
            return;
        }

        var copy = inventory.Create(item.Item, item.Count);

        CopyState(item, copy);
        bag.Place(home.Group, slot, copy);
        kept.Remove(item);

        inventory.TryAddGold(player, -price);

        Send(player, new ShopRepurchaserResponse { Items = [InventoryService.ToStorageValue(copy)] });
        Send(player, new ShopRepurchaserDeleteResponse { Serial = item.Serial });
    }

    /// <summary>
    /// What is short of a price in a currency, as the code of the refusal, or <c>null</c> when the person can pay.
    /// </summary>
    private ushort? Shortfall(Entity player, byte currency, long price, ItemRow item, short count)
    {
        switch ((ShopCurrency)currency)
        {
            case ShopCurrency.Gold:
            {
                // A shop never sells for gold below what the item sells for, or buying and selling would make money.
                if (IsSellable(item) && price < (long)count * item.SellPrice)
                {
                    return WrongPrice;
                }

                return inventory.Gold(player) < price ? NotEnoughMoney : null;
            }

            case ShopCurrency.Bp:
            case ShopCurrency.Ether:
            {
                return inventory.Balance(player, (ShopCurrency)currency) < price ? NotEnoughMoney : null;
            }

            case ShopCurrency.Token:
            {
                return inventory.CountOf(player, Token) < price ? NotEnoughTokens : null;
            }

            default:
            {
                return WrongPrice;
            }
        }
    }

    private void Pay(Entity player, byte currency, long price)
    {
        if (price <= 0)
        {
            return;
        }

        if (currency == TokenPrice)
        {
            inventory.TryTake(player, Token, (int)price);
            return;
        }

        inventory.TryAddCurrency(player, (ShopCurrency)currency, -price);
    }

    /// <summary>
    /// What an item was when it was sold, on the item that comes back.
    /// </summary>
    private static void CopyState(ItemInstance from, ItemInstance to)
    {
        to.BindState = from.BindState;
        to.Upgrade = from.Upgrade;
        to.Endurance = from.Endurance;
        to.SocketActive = from.SocketActive;
        to.DueTo = from.DueTo;
        to.UpgradeCount = from.UpgradeCount;
        to.UpgradeLimit = from.UpgradeLimit;
        to.Flag = from.Flag;
        to.Exp = from.Exp;
        to.BroachState = from.BroachState;
        to.RestoreCount = from.RestoreCount;
        to.SealCount = from.SealCount;
        to.Options = from.Options;
        to.Sockets = from.Sockets;
        to.Broaches = from.Broaches;
    }

    /// <summary>
    /// Whether a shop takes the item: a few kinds of items may never be sold to an NPC.
    /// </summary>
    private static bool IsSellable(ItemRow item) => item.SellLimit is not (1 or 3 or 6 or 7);

    private List<ItemInstance> Repurchasable(Entity player)
    {
        if (world.TryGet<ShopRepurchaseComponent>(player, out var kept) && kept.Items is not null)
        {
            return kept.Items;
        }

        var items = new List<ItemInstance>();

        world.Set(player, new ShopRepurchaseComponent { Items = items });

        return items;
    }

    private static MessageOpcode Opcode(ShopOpcode command) => MessageOpcode.From(GroupOpcode.Shop, command);

    private void Refuse(Entity player, MessageOpcode request, ushort code)
    {
        Send(player, new ErrorMessageResponse(request, code));
    }

    private void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(message);
        }
    }
}
