using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// What happens when an item is used: boxes and packages open into items and money.
/// </summary>
public sealed class ItemUseService(
    World world,
    InventoryService inventory,
    ItemCatalog catalog,
    ReadOnlyCollection<ItemPackageRow> packages,
    ReadOnlyCollection<ItemRandomBoxRow> randomBoxes)
{
    private static readonly StorageGroup[] Usable = [StorageGroup.Common, StorageGroup.Costume, StorageGroup.Cash, StorageGroup.Cube];

    /// <summary>
    /// What an item gives when it is opened: money and items with their counts.
    /// </summary>
    public readonly record struct Reward(long Gold, long BattlePoint, long Ether, List<(int Item, int Count)> Items);

    public bool Use(Entity player, StorageGroup group, short slot, long serial)
    {
        var bag = inventory.InventoryOf(player);
        var item = bag?.At(group, slot);

        if (bag is null || item is null || item.Serial != serial || !Usable.Contains(group) || catalog.Classify(item.Item) is not { } classify)
        {
            return false;
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
        foreach (var row in packages)
        {
            if (row.Id != id)
            {
                continue;
            }

            var ids = new[] { row.Field4, row.Field5, row.Field6, row.Field7, row.Field8, row.Field9, row.Field10, row.Field11, row.Field12, row.Field13 };
            var counts = new[] { row.Field24, row.Field25, row.Field26, row.Field27, row.Field28, row.Field29, row.Field30, row.Field31, row.Field32, row.Field33 };

            return new Reward(
                row.Field1,
                row.Field2,
                row.Field3,
                ids.Zip(counts).Where(e => e.First > 0 && e.Second > 0).ToList());
        }

        return null;
    }

    private Reward? OpenRandomBox(int id)
    {
        foreach (var row in randomBoxes)
        {
            if (row.Id != id)
            {
                continue;
            }

            var rates = new[] { row.Field7, row.Field8, row.Field9, row.Field10, row.Field11, row.Field12, row.Field13, row.Field14, row.Field15, row.Field16, row.Field17, row.Field18, row.Field19, row.Field20, row.Field21 };
            var ids = new[] { row.Field24, row.Field25, row.Field26, row.Field27, row.Field28, row.Field29, row.Field30, row.Field31, row.Field32, row.Field33, row.Field34, row.Field35, row.Field36, row.Field37, row.Field38 };
            var counts = new[] { row.Field39, row.Field40, row.Field41, row.Field42, row.Field43, row.Field44, row.Field45, row.Field46, row.Field47, row.Field48, row.Field49, row.Field50, row.Field51, row.Field52, row.Field53 };

            var total = Math.Max(1, row.Field2);
            var items = new List<(int, int)>();
            long gold = 0, battlePoint = 0, ether = 0;

            for (var roll = 0; roll < Math.Max(1, (int)row.Field1); roll++)
            {
                var point = Random.Shared.Next(total);

                if (point < row.Field4)
                {
                    gold += row.Field3;
                    continue;
                }

                point -= row.Field4;

                if (point < row.Field5)
                {
                    battlePoint += row.Field22;
                    continue;
                }

                point -= row.Field5;

                if (point < row.Field6)
                {
                    ether += row.Field23;
                    continue;
                }

                point -= row.Field6;

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
