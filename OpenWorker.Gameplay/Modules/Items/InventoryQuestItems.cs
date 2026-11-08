using Arch.Core;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// What quests see of the items of a person: the bags, without what the person wears.
/// </summary>
public sealed class InventoryQuestItems(InventoryService inventory) : IQuestItems
{
    public ValueTask<int> CountAsync(Entity player, int item, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((int)Math.Min(inventory.CountOf(player, item), int.MaxValue));
    }

    /// <summary>
    /// Takes as many of each item as the person has, up to the count.
    /// </summary>
    public ValueTask TakeAsync(Entity player, IReadOnlyList<QuestItem> items, CancellationToken cancellationToken)
    {
        foreach (var (item, count) in items)
        {
            var take = (int)Math.Min(inventory.CountOf(player, item), count);

            if (take > 0)
            {
                inventory.TryTake(player, item, take);
            }
        }

        return ValueTask.CompletedTask;
    }
}
