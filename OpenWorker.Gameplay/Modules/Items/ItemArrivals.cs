using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Tells the features that count items (the quests that ask a person to collect them) that items came into the bags of a
/// person, from wherever they came: a pickup, a purchase, a reward, a letter. <see cref="InventoryService"/> reports here
/// after every item it gives, so those features need not sit next to every place that gives items.
/// </summary>
/// <remarks>Runs on the world loop, like the inventory.</remarks>
public sealed class ItemArrivals
{
    private readonly List<Action<Entity, int, int>> _listeners = [];

    /// <summary>
    /// Calls <paramref name="listener"/> with the person, the item and how many came, after every gift of items.
    /// </summary>
    public void Listen(Action<Entity, int, int> listener)
    {
        _listeners.Add(listener);
    }

    public void Arrived(Entity player, int item, int count)
    {
        foreach (var listener in _listeners)
        {
            listener(player, item, count);
        }
    }
}
