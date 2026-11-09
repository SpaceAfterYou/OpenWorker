using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res.Types;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// What happens when an item is used: boxes and packages open into items and money.
/// </summary>
public sealed class ItemUseService(
    InventoryService inventory,
    ItemCatalog catalog,
    ReadOnlyCollection<ItemPackageRow> packages,
    ReadOnlyCollection<ItemRandomBoxRow> randomBoxes,
    BoosterManager? boosters = null)
{
    private readonly RowIndex<int, ItemPackageRow> _packages = packages.IndexBy(e => e.Id);
    private readonly RowIndex<int, ItemRandomBoxRow> _randomBoxes = randomBoxes.IndexBy(e => e.Id);

    // The kind of effect in the item row that names a booster.
    private const int BoosterEffect = 3;

    private static readonly StorageGroup[] Usable = [StorageGroup.Common, StorageGroup.Costume, StorageGroup.Cash, StorageGroup.Cube];

    /// <summary>
    /// What an item gives when it is opened: money and items with their counts.
    /// </summary>
    public readonly record struct Reward(long Gold, long BattlePoint, long Ether, List<(int Item, int Count)> Items);

    public async ValueTask<bool> UseAsync(Entity player, StorageGroup group, short slot, long serial, CancellationToken cancellationToken)
    {
        var bag = inventory.InventoryOf(player);
        var item = bag?.At(group, slot);

        if (bag is null || item is null || item.Serial != serial || !Usable.Contains(group) || catalog.Classify(item.Item) is not { } classify)
        {
            return false;
        }

        // A booster item gives its booster, which the item row names; the item is used up when the person gets it.
        if (classify.ItemUseType == ItemUseType.BOOSTER)
        {
            if (boosters is null || !catalog.TryGet(item.Item, out var boosterRow) || boosterRow.EffectType != BoosterEffect ||
                !await boosters.AddAsync(player, (short)boosterRow.EffectValue, null, cancellationToken))
            {
                return false;
            }

            inventory.Spend(player, item, 1);

            return true;
        }

        var reward = classify.ItemUseType switch
        {
            ItemUseType.PACKAGE or ItemUseType.PACKAGE_COSTUME => OpenPackage(item.Item),
            ItemUseType.RANDOM_BOX => OpenRandomBox(item.Item),
            _ => null
        };

        if (reward is not { } opened)
        {
            return false;
        }

        // The box goes only when everything that comes out of it fits.
        if (!inventory.CanGiveAll(player, opened.Items, ignoring: item))
        {
            return false;
        }

        inventory.Spend(player, item, 1);

        Grant(player, opened);

        return true;
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
