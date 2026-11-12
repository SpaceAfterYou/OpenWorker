using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Gameplay.Modules.Tower;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// The answer to the use of an item: it was used, or the code of the error the client is told. A used item that has a motion
/// (a potion) is shown to everybody who sees the person.
/// </summary>
/// <param name="Error">Zero when the item was used.</param>
/// <param name="Item">The item that was used.</param>
/// <param name="Slot">The slot of the bag it was in.</param>
/// <param name="Motion">The use plays a motion and starts the cooldown in the bag of the client.</param>
public readonly record struct ItemUseResult(ushort Error, int Item = 0, short Slot = 0, bool Motion = false)
{
    public bool Ok => Error == 0;

    public static ItemUseResult Used(int item = 0, short slot = 0, bool motion = false) => new(0, item, slot, motion);

    public static ItemUseResult Refused(ushort code = ItemUseCodes.NotUsable) => new(code);
}

/// <summary>
/// The codes the client is told when the use of an item is refused, as the original server gives them.
/// </summary>
public static class ItemUseCodes
{
    /// <summary>The item is not there, not usable, or not usable now: the code for everything that has no other.</summary>
    public const ushort NotUsable = InventoryService.Refused;

    /// <summary>The item is for another class.</summary>
    public const ushort OtherClass = 52009;

    /// <summary>The bags have no room for what an opened box gives.</summary>
    public const ushort BagFull = 52010;

    /// <summary>A fallen person cannot use an item.</summary>
    public const ushort Fallen = 52026;

    /// <summary>The person has not the level the item asks for.</summary>
    public const ushort LevelTooLow = 52601;

    /// <summary>The item (its group) is still cooling down.</summary>
    public const ushort Cooling = 52602;

    /// <summary>An item that only gets a person up is used where nobody can get up.</summary>
    public const ushort NoRevive = 52607;

    /// <summary>The kind of item has been used as often as a day allows.</summary>
    public const ushort DailyLimit = 52610;

    /// <summary>The infinite tower takes no item.</summary>
    public const ushort InfiniteTower = 52612;

    /// <summary>The item of a tower's ticket is not usable.</summary>
    public const ushort NoTowerTicket = 52613;

    /// <summary>The item cannot be used in this kind of place.</summary>
    public const ushort WrongPlace = 52616;
}

/// <summary>
/// What happens when an item is used (08/11): the checks every item goes through, in the order of the original server, then
/// what its kind does. Boxes and packages open into items and money, an akashic record card goes into the book, an item of
/// fatigue fills the fatigue of the day, an item of a title gives the title, a potion gives its buff in a maze, a booster its
/// booster. Every kind that is used starts the cooldown of the item's group.
/// </summary>
public sealed class ItemUseService(
    World world,
    InventoryService inventory,
    ItemCatalog catalog,
    ItemUseLimits limits,
    ReadOnlyCollection<ItemPackageRow> packages,
    ReadOnlyCollection<ItemRandomBoxRow> randomBoxes,
    ReadOnlyCollection<ItemClassrewardLinkRow> classRewards,
    IItemField? field = null,
    IItemTitles? titles = null,
    BoosterManager? boosters = null,
    AkashicService? akashic = null,
    FatigueService? fatigue = null,
    IReviveItems? revive = null,
    AchievementManager? achievements = null,
    InfiniteTower? tower = null)
{
    private readonly RowIndex<int, ItemPackageRow> _packages = packages.IndexBy(e => e.Id);
    private readonly RowIndex<int, ItemRandomBoxRow> _randomBoxes = randomBoxes.IndexBy(e => e.Id);
    private readonly RowIndex<int, ItemClassrewardLinkRow> _classRewards = classRewards.IndexBy(e => (int)e.Id);

    // The kind of effect in the item row that names a booster, a buff, an aura and a title.
    private const int BoosterEffect = 3;
    private const int BuffEffect = 1;
    private const int AuraEffect = 2;
    private const int TitleEffect = 4;
    private const int ClassBoxEffect = 7;

    // The group of the classify table that holds the items taken at a meal: potions and fatigue items.
    private const int ConsumableGroup = 17;

    // What a kind of item may do where, in the bits of the use state of its classify row.
    private const int UsableInDistrict = 1;
    private const int UsableInMaze = 2;
    private const int UsableWhenFallen = 8;

    // The slot types of the items that open a memory place of the equipment: the first memory place, then the ten places.
    private const int FirstMemorySlotType = 101;
    private const int MemorySlots = 10;

    // The most a client lists in a reward: it counts the items in a byte.
    private const int MostGained = byte.MaxValue;

    // The calls that get a fallen player up are items of the sub type 2 whose effect is 1 (the player itself).
    private const int ReviveSubType = 2;
    private const int SelfReviveEffect = 1;

    private static readonly StorageGroup[] Usable = [StorageGroup.Common, StorageGroup.Costume, StorageGroup.Cash, StorageGroup.Cube];

    // Boxes are opened from the common bag and the cash bag only.
    private static readonly StorageGroup[] BoxStorages = [StorageGroup.Common, StorageGroup.Cash];

    private readonly IItemField _field = field ?? new DistrictItemField();

    /// <summary>
    /// What an item gives when it is opened: money and items with their counts.
    /// </summary>
    public readonly record struct Reward(long Gold, long BattlePoint, long Ether, List<(int Item, int Count)> Items);

    /// <summary>
    /// Uses the item in a slot of a bag. The client names the slot and the serial of the item it believes is there.
    /// </summary>
    public async ValueTask<ItemUseResult> UseAsync(Entity player, StorageGroup group, short slot, long serial, CancellationToken cancellationToken)
    {
        var bag = inventory.InventoryOf(player);
        var item = bag?.At(group, slot);

        if (bag is null || item is null || item.Serial != serial || !Usable.Contains(group) ||
            !catalog.TryGet(item.Item, out var row) || catalog.Classify(item.Item) is not { } classify)
        {
            return ItemUseResult.Refused();
        }

        var place = _field.PlaceOf(player);

        // What every item asks, in this order.
        if (row.LevelLimit > world.Get<LevelComponent>(player).Level)
        {
            return ItemUseResult.Refused(ItemUseCodes.LevelTooLow);
        }

        if (row.ClassLimit != 0 && row.ClassLimit != (byte)world.Get<PersonInfoComponent>(player).Hero)
        {
            return ItemUseResult.Refused(ItemUseCodes.OtherClass);
        }

        if (place.Down && (classify.UseState & UsableWhenFallen) == 0)
        {
            return ItemUseResult.Refused(ItemUseCodes.Fallen);
        }

        if (limits.IsCooling(player, row))
        {
            return ItemUseResult.Refused(ItemUseCodes.Cooling);
        }

        if (item.Count < 1 || !place.ModeAllowsItems)
        {
            return ItemUseResult.Refused();
        }

        var result = classify.ItemUseType switch
        {
            ItemUseType.BOOSTER => await UseBoosterAsync(player, bag, item, row, cancellationToken),
            ItemUseType.TITLE => await UseTitleAsync(player, item, row),
            ItemUseType.AKASHIC_RECORD => UseAkashic(player, item, classify),
            ItemUseType.FP or ItemUseType.FP_FREE => UseFatigue(player, item, row, classify),
            ItemUseType.PACKAGE or ItemUseType.PACKAGE_COSTUME or ItemUseType.RANDOM_BOX or ItemUseType.BOX_CLASS =>
                UseBox(player, item, row, classify),
            ItemUseType.EFFECT => UseEffect(player, item, row, classify, place),
            ItemUseType.EQUIP_SLOT_OPEN => OpenEquipSlot(player, item, classify),
            ItemUseType.INFINITE_TOWER => UseTowerTicket(player, item),
            _ => ItemUseResult.Refused()
        };

        if (result.Ok)
        {
            limits.StartCooldown(player, row);
            achievements?.Count(player, AchievementType.ItemUsed);
        }

        return result;
    }

    /// <summary>
    /// The item in a slot that a service of its own takes: the person's request names the slot, not the use of an item (a
    /// skill reset, a new place in the deck). Null when there is no item of that use type in the slot.
    /// </summary>
    public ItemInstance? ItemOfService(Entity player, StorageGroup group, short slot, ItemUseType type)
    {
        var item = inventory.InventoryOf(player)?.At(group, slot);

        return item is { Count: >= 1 } && Usable.Contains(group) && catalog.Classify(item.Item) is { } classify && classify.ItemUseType == type
            ? item
            : null;
    }

    /// <summary>
    /// A ticket of the infinite tower clears the count of the day once its entries are used up, and is used up itself.
    /// </summary>
    private ItemUseResult UseTowerTicket(Entity player, ItemInstance item)
    {
        if (tower is null)
        {
            return ItemUseResult.Refused(ItemUseCodes.NoTowerTicket);
        }

        if (tower.UseTicket(player) is { } refused)
        {
            return ItemUseResult.Refused(refused);
        }

        inventory.Spend(player, item, 1);

        return ItemUseResult.Used();
    }

    /// <summary>
    /// A booster item gives its booster, which the item row names; the item is used up when the person gets it.
    /// </summary>
    private async ValueTask<ItemUseResult> UseBoosterAsync(Entity player, Inventory bag, ItemInstance item, ItemRow row, CancellationToken cancellationToken)
    {
        if (boosters is null || row.EffectType != BoosterEffect || !await boosters.AddAsync(player, (short)row.EffectValue, null, cancellationToken))
        {
            return ItemUseResult.Refused();
        }

        // The booster took a while to give: the item must still be the one that was used.
        if (!world.IsAlive(player) || bag.At(item.Storage, item.Slot) != item)
        {
            return ItemUseResult.Refused();
        }

        inventory.Spend(player, item, 1);

        return ItemUseResult.Used();
    }

    /// <summary>
    /// An item of a title gives the title, unless the person has it or cannot have it.
    /// </summary>
    private async ValueTask<ItemUseResult> UseTitleAsync(Entity player, ItemInstance item, ItemRow row)
    {
        var granter = titles ?? new NoItemTitles();

        if (row.EffectType != TitleEffect || granter.CannotGrant(player, row.EffectValue))
        {
            return ItemUseResult.Refused();
        }

        var bag = inventory.InventoryOf(player);

        if (!await granter.GrantAsync(player, row.EffectValue) || !world.IsAlive(player) || bag?.At(item.Storage, item.Slot) != item)
        {
            return ItemUseResult.Refused();
        }

        inventory.Spend(player, item, 1);
        Tell(player, new ItemUseResponse { Item = item.Item, Gain = new ItemUseGainValue { Items = [] } });

        return ItemUseResult.Used();
    }

    /// <summary>
    /// An item of a place of the equipment opens that place, if it is not open yet. The item comes from the common bag.
    /// </summary>
    private ItemUseResult OpenEquipSlot(Entity player, ItemInstance item, ItemClassifyRow classify)
    {
        var bit = classify.SlotType - FirstMemorySlotType;

        if (item.Storage != StorageGroup.Common || bit is < 0 or >= MemorySlots || limits.IsEquipSlotOpen(player, bit))
        {
            return ItemUseResult.Refused();
        }

        inventory.Spend(player, item, 1);
        limits.OpenEquipSlot(player, bit);

        return ItemUseResult.Used();
    }

    /// <summary>
    /// An akashic record card goes into the book of the person.
    /// </summary>
    private ItemUseResult UseAkashic(Entity player, ItemInstance item, ItemClassifyRow classify)
    {
        if (akashic is null || !akashic.Register(player, item))
        {
            return ItemUseResult.Refused();
        }

        limits.CountUse(player, (int)classify.ItemUseType);

        return ItemUseResult.Used();
    }

    /// <summary>
    /// An item of fatigue adds its value to the fatigue of the day, unless the person has saved fatigue or a full day. The
    /// ones that are not free may be used three times a game day.
    /// </summary>
    private ItemUseResult UseFatigue(Entity player, ItemInstance item, ItemRow row, ItemClassifyRow classify)
    {
        var type = (int)classify.ItemUseType;

        if (classify.Group != ConsumableGroup || fatigue is null)
        {
            return ItemUseResult.Refused();
        }

        if (classify.ItemUseType == ItemUseType.FP && !limits.CanUseKind(player, type))
        {
            return ItemUseResult.Refused(ItemUseCodes.DailyLimit);
        }

        if (row.UseValue == 0)
        {
            return ItemUseResult.Refused();
        }

        if (fatigue.Restore(player, row.UseValue) is { } refusal)
        {
            return ItemUseResult.Refused(refusal);
        }

        inventory.Spend(player, item, 1);
        limits.CountUse(player, type);

        return ItemUseResult.Used();
    }

    /// <summary>
    /// A box or a package opens into what its table holds; it goes only when everything that comes out of it fits, and the
    /// client is shown what came.
    /// </summary>
    private ItemUseResult UseBox(Entity player, ItemInstance item, ItemRow row, ItemClassifyRow classify)
    {
        if (!BoxStorages.Contains(item.Storage))
        {
            return ItemUseResult.Refused();
        }

        var kind = classify.ItemUseType;
        var box = item.Item;

        // A box of a class opens the box its table names for the class of the person.
        if (kind == ItemUseType.BOX_CLASS)
        {
            if (row.EffectType != ClassBoxEffect || !_classRewards.TryGet(row.EffectValue, out var link) ||
                ClassBox(link, world.Get<PersonInfoComponent>(player).Hero) is not { } inner || catalog.Classify(inner) is not { } innerClassify)
            {
                return ItemUseResult.Refused();
            }

            box = inner;
            kind = innerClassify.ItemUseType;
        }

        var reward = kind switch
        {
            ItemUseType.PACKAGE or ItemUseType.PACKAGE_COSTUME => OpenPackage(box),
            ItemUseType.RANDOM_BOX => OpenRandomBox(box),
            _ => null
        };

        if (reward is not { } opened)
        {
            return ItemUseResult.Refused();
        }

        if (!inventory.CanGiveAll(player, opened.Items, ignoring: item))
        {
            return ItemUseResult.Refused(ItemUseCodes.BagFull);
        }

        // What the box holds has to fit the purses as well as the bags.
        if (!inventory.CanReceive(player, ShopCurrency.Gold, opened.Gold))
        {
            return ItemUseResult.Refused(InventoryService.TooMuchGold);
        }

        if (!inventory.CanReceive(player, ShopCurrency.Bp, opened.BattlePoint))
        {
            return ItemUseResult.Refused(InventoryService.TooMuchBattlePoints);
        }

        if (!inventory.CanReceive(player, ShopCurrency.Ether, opened.Ether))
        {
            return ItemUseResult.Refused(InventoryService.TooMuchEther);
        }

        inventory.Spend(player, item, 1);

        Grant(player, opened);

        Tell(player, new ItemUseResponse
        {
            Item = item.Item,
            Gain = new ItemUseGainValue
            {
                Gold = (int)Math.Clamp(opened.Gold, 0, int.MaxValue),
                BattlePoint = (int)Math.Clamp(opened.BattlePoint, 0, int.MaxValue),
                Ether = (int)Math.Clamp(opened.Ether, 0, int.MaxValue),
                Items = opened.Items.Take(MostGained).Select(e => new ItemUseGainItem { Item = e.Item, Count = (short)Math.Clamp(e.Count, 0, short.MaxValue) }).ToArray()
            }
        });

        return ItemUseResult.Used();
    }

    /// <summary>
    /// A potion or the like: in a maze it gives its buff to the person. A place the kind of item is not for and the infinite
    /// tower are refused; so are the items that bring a person back, but for the call that gets the person itself up.
    /// </summary>
    private ItemUseResult UseEffect(Entity player, ItemInstance item, ItemRow row, ItemClassifyRow classify, ItemPlace place)
    {
        if (classify.Group != ConsumableGroup)
        {
            return ItemUseResult.Refused();
        }

        // Where the kind may be used: the place has to take items, and the use state of the kind has to name the place.
        var bit = place.InMaze ? UsableInMaze : UsableInDistrict;

        if (!place.ItemsAllowed || (classify.UseState & bit) == 0)
        {
            return ItemUseResult.Refused(ItemUseCodes.WrongPlace);
        }

        if (place.Infinite)
        {
            return ItemUseResult.Refused(ItemUseCodes.InfiniteTower);
        }

        // A call that gets a person up is used from the window of the fallen: the maze says whether it does.
        if (row.SubType == ReviveSubType && row.EffectType == SelfReviveEffect && revive is not null)
        {
            return revive.Use(player, item);
        }

        // The other items that get a person up are not for this request.
        if (row.SubType != 0)
        {
            return ItemUseResult.Refused(ItemUseCodes.NoRevive);
        }

        var given = row.EffectType switch
        {
            BuffEffect => _field.GiveBuff(player, row.EffectValue),
            AuraEffect => _field.GiveAura(player, row.EffectValue),
            _ => false
        };

        if (!given)
        {
            return ItemUseResult.Refused();
        }

        // The stack may go with its last item: the motion names the slot it was in.
        var slot = item.Slot;

        inventory.Spend(player, item, 1);

        return ItemUseResult.Used(item.Item, slot, motion: true);
    }

    /// <summary>
    /// The box the table names for a hero class, if any.
    /// </summary>
    private static int? ClassBox(ItemClassrewardLinkRow link, Hero hero)
    {
        var box = (int)hero switch
        {
            1 => link.Field1,
            2 => link.Field2,
            3 => link.Field3,
            4 => link.Field4,
            5 => link.Field5,
            6 => link.Field6,
            _ => 0
        };

        return box > 0 ? box : null;
    }

    private void Tell(Entity player, ItemUseResponse answer)
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(answer);
        }
    }

    /// <summary>
    /// Gives a reward: items to the inventory, money to the wallet.
    /// </summary>
    public void Grant(Entity player, Reward reward)
    {
        foreach (var (id, count) in reward.Items)
        {
            inventory.TryGive(player, id, count);
        }

        if (reward.Gold != 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Gold, reward.Gold);
        }

        if (reward.BattlePoint != 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Bp, reward.BattlePoint);
        }

        if (reward.Ether != 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Ether, reward.Ether);
        }
    }

    private Reward? OpenPackage(int id)
    {
        if (_packages.TryGet(id, out var row))
        {
            var ids = new[] { row.Item1, row.Item2, row.Item3, row.Item4, row.Item5, row.Item6, row.Item7, row.Item8, row.Item9, row.Item10 };
            var counts = new[] { row.Count1, row.Count2, row.Count3, row.Count4, row.Count5, row.Count6, row.Count7, row.Count8, row.Count9, row.Count10 };

            return new Reward(
                row.Gold,
                row.BattlePoint,
                row.Ether,
                ids.Zip(counts).Where(e => e.First > 0 && e.Second > 0).ToList());
        }

        return null;
    }

    private Reward? OpenRandomBox(int id)
    {
        if (_randomBoxes.TryGet(id, out var row))
        {
            var rates = new[] { row.ItemRate1, row.ItemRate2, row.ItemRate3, row.ItemRate4, row.ItemRate5, row.ItemRate6, row.ItemRate7, row.ItemRate8, row.ItemRate9, row.ItemRate10, row.ItemRate11, row.ItemRate12, row.ItemRate13, row.ItemRate14, row.ItemRate15 };
            var ids = new[] { row.Item1, row.Item2, row.Item3, row.Item4, row.Item5, row.Item6, row.Item7, row.Item8, row.Item9, row.Item10, row.Item11, row.Item12, row.Item13, row.Item14, row.Item15 };
            var counts = new[] { row.Count1, row.Count2, row.Count3, row.Count4, row.Count5, row.Count6, row.Count7, row.Count8, row.Count9, row.Count10, row.Count11, row.Count12, row.Count13, row.Count14, row.Count15 };

            var total = Math.Max(1, row.TotalRate);
            var items = new List<(int, int)>();
            long gold = 0, battlePoint = 0, ether = 0;

            for (var roll = 0; roll < Math.Max(1, (int)row.Rolls); roll++)
            {
                var point = Random.Shared.Next(total);

                if (point < row.GoldRate)
                {
                    gold += row.Gold;
                    continue;
                }

                point -= row.GoldRate;

                if (point < row.BattlePointRate)
                {
                    battlePoint += row.BattlePoint;
                    continue;
                }

                point -= row.BattlePointRate;

                if (point < row.EtherRate)
                {
                    ether += row.Ether;
                    continue;
                }

                point -= row.EtherRate;

                for (var i = 0; i < rates.Length; i++)
                {
                    if (point < rates[i])
                    {
                        if (ids[i] > 0 && counts[i] > 0)
                        {
                            items.Add((ids[i], counts[i]));
                        }

                        break;
                    }

                    point -= rates[i];
                }
            }

            return new Reward(gold, battlePoint, ether, items);
        }

        return null;
    }
}
