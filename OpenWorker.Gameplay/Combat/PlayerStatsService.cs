using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// Tells the player what its stats are now, after the level or the gear has changed.
/// </summary>
public sealed class PlayerStatsService(World world, CombatTables combat)
{
    public CombatStats Of(Entity player)
    {
        return combat.GetPlayer(world, player);
    }

    public void Send(Entity player)
    {
        if (!world.IsAlive(player) || !world.Has<ServerSessionComponent>(player))
        {
            return;
        }

        world.Get<ServerSessionComponent>(player).Send(new PersonUpdateOriginStatListResponse
        {
            Actor = world.Get<ActorComponent>(player),
            Stats = Of(player).ToStatList()
        });
    }
}
