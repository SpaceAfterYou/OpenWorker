using Arch.Core;
using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Lua.Managers;

public sealed partial class LuaCreatureManager
{
    /// <summary>
    /// A monster of a box comes back after its time: the same monster, made again at a new place of the box and shown to everybody
    /// at once. Nothing for a monster the box does not hold, a townsman, or a maze that is over.
    /// </summary>
    public Entity? Reappear(int identifier, MonsterSpawnBox spawn)
    {
        if (Closed || identifier == 0)
        {
            return null;
        }

        foreach (var slot in spawn.MonsterList)
        {
            if (slot.Id != identifier || slot.Type == MonsterSpawnType.Npc)
            {
                continue;
            }

            var created = new List<Entity>();

            CreateOf(spawn, slot, created);

            if (created.Count == 0)
            {
                return null;
            }

            Show(created);

            return created[0];
        }

        return null;
    }
}
