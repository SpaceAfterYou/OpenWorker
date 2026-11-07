using Arch.Core;
using OpenWorker.Gameplay.Modules.Items.Components;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Removes the storages and items of a player whose connection has closed, which are entities of
/// their own and would otherwise stay in the world.
/// </summary>
public sealed class PlayerStorageCleaner(World world) : ISessionCloseHandler
{
    public ValueTask OnSessionClosedAsync(Entity player)
    {
        if (!world.Has<StorageComponent>(player))
        {
            return ValueTask.CompletedTask;
        }

        foreach (var storage in world.Get<StorageComponent>(player).Collection ?? [])
        {
            if (!world.IsAlive(storage))
            {
                continue;
            }

            foreach (var item in world.Get<StorageContentComponent>(storage).SlotList ?? [])
            {
                if (item != Entity.Null && world.IsAlive(item))
                {
                    world.Destroy(item);
                }
            }

            world.Destroy(storage);
        }

        return ValueTask.CompletedTask;
    }
}
