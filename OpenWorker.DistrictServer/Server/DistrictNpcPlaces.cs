using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Npcs;

namespace OpenWorker.DistrictServer.Server;

/// <summary>
/// The NPCs the map of this city places, by the actor each was given. They stand in every channel of the city alike.
/// </summary>
public sealed class DistrictNpcPlaces(World world, NpcManager npcs) : INpcPlaces
{
    private readonly Dictionary<int, Entity> _byActor = npcs.Collection
        .Where(e => world.IsAlive(e) && world.Has<ActorComponent>(e))
        .GroupBy(e => world.Get<ActorComponent>(e).Identifier)
        .ToDictionary(e => e.Key, e => e.First());

    public bool TryFind(Entity player, int actor, out int table, out Vector3 position)
    {
        table = 0;
        position = default;

        if (!_byActor.TryGetValue(actor, out var entity) || !world.IsAlive(entity) ||
            !world.TryGet<NpcComponent>(entity, out var npc) || npc is null)
        {
            return false;
        }

        table = npc.Prototype;
        position = npc.Position;

        return true;
    }
}
