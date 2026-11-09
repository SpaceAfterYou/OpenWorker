using Arch.Core;
using OpenWorker.Gameplay.Combat;

namespace OpenWorker.Lua.Managers;

/// <summary>
/// The greatest life of a player in a maze is kept by the vitals of its maze, which tell the party when it changes.
/// </summary>
public sealed class MazeHealthResize(World world) : IPlayerHealthResize
{
    public bool Resize(Entity player, int maximum)
    {
        if (!world.IsAlive(player) || !world.TryGet<LuaMaze>(player, out var maze) || maze is null)
        {
            return false;
        }

        maze.Creatures.Vitals.Resize(player, maximum);

        return true;
    }
}
