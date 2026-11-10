using System.Numerics;
using Arch.Core;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The NPCs a maze places are its townsfolk: the NPC a request names is one of the creatures of the maze the player stands
/// in, found by the actor the maze gave it. District 6 is such a maze, with the NPCs of transports.
/// </summary>
public sealed class MazeNpcPlaces(World world) : INpcPlaces
{
    public bool TryFind(Entity player, int actor, out int table, out Vector3 position)
    {
        table = 0;
        position = default;

        if (!world.IsAlive(player) || !world.TryGet(player, out LuaMaze? maze) || maze is null || maze.IsClosed ||
            !maze.Creatures.TryFindNpc(actor, out var npc))
        {
            return false;
        }

        table = world.Get<CreatureComponent>(npc).Identifier;
        position = world.Get<WorldComponent>(npc).Position;

        return true;
    }
}
