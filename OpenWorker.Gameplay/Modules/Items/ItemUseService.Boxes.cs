using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
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

    // The kind of effect in the item row that names a class box.
    private const int ClassBoxEffect = 7;

    // The most a client lists in a reward: it counts the items in a byte.
    private const int MostGained = byte.MaxValue;

    // Boxes are opened from the common bag and the cash bag only.
    private static readonly StorageGroup[] BoxStorages = [StorageGroup.Common, StorageGroup.Cash];

    /// <summary>
    /// What an item gives when it is opened: money and items with their counts.
    /// </summary>
    public readonly record struct Reward(long Gold, long BattlePoint, long Ether, List<(int Item, int Count)> Items);

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
