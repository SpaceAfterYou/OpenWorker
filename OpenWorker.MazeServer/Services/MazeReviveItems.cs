using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The revive items of the maze: a call a fallen player uses from the window of the revive. It is spent when the player gets
/// up, and a maze lets a player use only so many.
/// </summary>
public sealed class MazeReviveItems(World world, InventoryService inventory, MazeRevival revival) : IReviveItems
{
    public ItemUseResult Use(Entity player, ItemInstance item)
    {
        if (!world.Has<LuaMaze>(player))
        {
            return ItemUseResult.Refused(ItemUseCodes.NoRevive);
        }

        var maze = world.Get<LuaMaze>(player);

        // A call used by a player on its feet is no use of a revive item.
        if (revival.CheckItem(player, maze.Maze, fallen: !maze.Creatures.Vitals.IsAlive(player)) is { } refused)
        {
            return ItemUseResult.Refused(refused == MazeRevival.NotFallen ? ItemUseCodes.NoRevive : refused);
        }

        var slot = item.Slot;

        inventory.Spend(player, item, 1);
        revival.ItemUsed(player);
        maze.Creatures.Revive(player, ReviveType.Buff, world.Get<ActorComponent>(player).Identifier);

        // The revive shows to the maze by itself: the use plays no motion of its own.
        return ItemUseResult.Used(item.Item, slot);
    }
}
