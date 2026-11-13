using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

public sealed partial class ItemUseService
{
    private readonly RowIndex<int, ItemPackageRow> _packages = packages.IndexBy(e => e.Id);
    private readonly RowIndex<int, ItemRandomBoxRow> _randomBoxes = randomBoxes.IndexBy(e => e.Id);
    private readonly RowIndex<int, ItemClassrewardLinkRow> _classRewards = classRewards.IndexBy(e => (int)e.Id);
    private readonly RowIndex<int, ItemPackageUnpackRow> _unpacking = (unpacking ?? new([])).IndexBy(e => e.Id);
    private readonly RowIndex<int, ReinforceRow> _reinforces = (reinforces ?? new([])).IndexBy(e => e.Id);

    // A package names an item it cannot give upgraded.
    private const ushort CannotUnpack = 52013;

    // The highest level an item is given at by a package.
    private const int MostUpgrade = 9;

    // The kind of effect in the item row that names a class box.
    private const int ClassBoxEffect = 7;

    // The most a client lists in a reward: it counts the items in a byte.
    private const int MostGained = byte.MaxValue;

    // Boxes are opened from the common bag and the cash bag only.
    private static readonly StorageGroup[] BoxStorages = [StorageGroup.Common, StorageGroup.Cash];

    /// <summary>
    /// What an item gives when it is opened: money and items with their counts, an item given upgraded with its level.
    /// </summary>
    public readonly record struct Reward(long Gold, long BattlePoint, long Ether, List<(int Item, int Count, byte Upgrade)> Items);

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

        var refusal = ItemUseCodes.NotUsable;
        var reward = kind switch
        {
            ItemUseType.PACKAGE or ItemUseType.PACKAGE_COSTUME => OpenPackage(box, out refusal),
            ItemUseType.RANDOM_BOX => OpenRandomBox(box),
            _ => null
        };

        if (reward is not { } opened)
        {
            return ItemUseResult.Refused(refusal);
        }

        if (!inventory.CanGiveAll(player, opened.Items.Select(e => (e.Item, e.Count)).ToList(), ignoring: item))
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

    /// <summary>
    /// Gives a reward: items to the inventory, money to the wallet.
    /// </summary>
    public void Grant(Entity player, Reward reward)
    {
        foreach (var (id, count, upgrade) in reward.Items)
        {
            if (upgrade == 0)
            {
                inventory.TryGive(player, id, count);
            }
            else
            {
                GiveUpgraded(player, id, upgrade);
            }
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

    /// <summary>
    /// One item at a level, as a package gives it: an item that does not stack, made and then raised to the level.
    /// </summary>
    private void GiveUpgraded(Entity player, int id, byte upgrade)
    {
        if (!inventory.TryGive(player, id, 1, out var changes, notify: false) || inventory.InventoryOf(player) is not { } bag)
        {
            return;
        }

        foreach (var change in changes)
        {
            if (bag.At(change.Storage, change.Slot) is { } made)
            {
                made.Upgrade = upgrade;
                bag.Touch(made);

                if (world.Has<ServerSessionComponent>(player))
                {
                    world.Get<ServerSessionComponent>(player).Send(new ItemCreateResponse { List = [InventoryService.ToStorageValue(made)] });
                }
            }
        }
    }

    /// <summary>
    /// What a package holds. The server's table says how each item is given: as it is, with its count, or as one item
    /// upgraded to the level its count names, which only an item that does not stack and can be upgraded that far can be.
    /// </summary>
    private Reward? OpenPackage(int id, out ushort refusal)
    {
        refusal = ItemUseCodes.NotUsable;

        if (!_packages.TryGet(id, out var row))
        {
            return null;
        }

        int[] ids = [row.Item1, row.Item2, row.Item3, row.Item4, row.Item5, row.Item6, row.Item7, row.Item8, row.Item9, row.Item10, row.Item11, row.Item12, row.Item13, row.Item14, row.Item15];
        int[] counts = [row.Count1, row.Count2, row.Count3, row.Count4, row.Count5, row.Count6, row.Count7, row.Count8, row.Count9, row.Count10, row.Count11, row.Count12, row.Count13, row.Count14, row.Count15];

        var unpack = _unpacking.TryGet(id, out var server) ? server : (ItemPackageUnpackRow?)null;
        var items = new List<(int, int, byte)>();

        for (var i = 0; i < ids.Length; i++)
        {
            if (ids[i] <= 0 || counts[i] <= 0)
            {
                continue;
            }

            // The server's row counts only where it names the same item as the client's.
            if (unpack is not { } kinds || kinds.Items[i] != ids[i] || kinds.Kinds[i] != ItemPackageUnpackRow.Upgraded)
            {
                items.Add((ids[i], counts[i], 0));
                continue;
            }

            var level = (byte)counts[i];

            if (catalog.StackLimit(ids[i]) != 1 || !catalog.TryGet(ids[i], out var given) || given.Reinforce == 0 ||
                !_reinforces.TryGet(given.Reinforce, out var reinforce) || reinforce.MaxLevel < level || level > MostUpgrade)
            {
                refusal = CannotUnpack;

                return null;
            }

            items.Add((ids[i], 1, level));
        }

        return new Reward(row.Gold, row.BattlePoint, row.Ether, items);
    }

    private Reward? OpenRandomBox(int id)
    {
        if (_randomBoxes.TryGet(id, out var row))
        {
            var rates = new[] { row.ItemRate1, row.ItemRate2, row.ItemRate3, row.ItemRate4, row.ItemRate5, row.ItemRate6, row.ItemRate7, row.ItemRate8, row.ItemRate9, row.ItemRate10, row.ItemRate11, row.ItemRate12, row.ItemRate13, row.ItemRate14, row.ItemRate15 };
            var ids = new[] { row.Item1, row.Item2, row.Item3, row.Item4, row.Item5, row.Item6, row.Item7, row.Item8, row.Item9, row.Item10, row.Item11, row.Item12, row.Item13, row.Item14, row.Item15 };
            var counts = new[] { row.Count1, row.Count2, row.Count3, row.Count4, row.Count5, row.Count6, row.Count7, row.Count8, row.Count9, row.Count10, row.Count11, row.Count12, row.Count13, row.Count14, row.Count15 };

            var total = Math.Max(1, row.TotalRate);
            var items = new List<(int, int, byte)>();
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
                            items.Add((ids[i], counts[i], 0));
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
