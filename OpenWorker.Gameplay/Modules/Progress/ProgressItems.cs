using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// The items of the rewards of achievements and of share points: a reward names an item, or a row of <c>tb_Quest_Reward</c>
/// that has an item for each class. They are given through <see cref="InventoryService"/>, all of them or none, so the dirty
/// mask of the inventory carries them to the flush.
/// </summary>
public sealed class ProgressItems(World world, InventoryService inventory, ReadOnlyCollection<QuestRewardRow> classRewards)
{
    private readonly RowIndex<int, QuestRewardRow> _classRewards = classRewards.IndexBy(e => (int)e.Id);

    /// <summary>
    /// The items the person gets: those of its class for a reward by class. An item the item tables do not have is left out,
    /// as is a reward by class with no item for the class.
    /// </summary>
    public List<(int Item, int Count)> Resolve(Entity player, IEnumerable<ProgressItem> rewards)
    {
        var hero = world.TryGet<PersonInfoComponent>(player, out var info) ? info.Hero : 0;
        var items = new List<(int Item, int Count)>();

        foreach (var reward in rewards)
        {
            var item = reward.Kind == ProgressItem.ByClass ? ForClass(reward.Item, hero) : reward.Item;

            if (item > 0 && reward.Count > 0 && inventory.Catalog.Exists(item))
            {
                items.Add((item, reward.Count));
            }
        }

        return items;
    }

    public bool CanGive(Entity player, IReadOnlyCollection<(int Item, int Count)> items)
    {
        return items.Count == 0 || inventory.CanGiveAll(player, items);
    }

    /// <summary>
    /// Gives all of the items or none of them.
    /// </summary>
    public bool TryGive(Entity player, IReadOnlyCollection<(int Item, int Count)> items)
    {
        if (!CanGive(player, items))
        {
            return false;
        }

        foreach (var (item, count) in items)
        {
            inventory.TryGive(player, item, count);
        }

        return true;
    }

    private int ForClass(int row, Hero hero)
    {
        if (!_classRewards.TryGet(row, out var reward))
        {
            return 0;
        }

        return hero switch
        {
            Hero.Haru => reward.HaruItem,
            Hero.Erwin => reward.ErwinItem,
            Hero.Lily => reward.LilyItem,
            Hero.Jin => reward.JinItem,
            Hero.Stella => reward.StellaItem,
            Hero.Iris => reward.IrisItem,
            _ => 0
        };
    }
}
