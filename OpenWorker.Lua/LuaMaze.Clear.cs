using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Gameplay.Messages.Response.Person.Components;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// The maze is won: its last sector has been cleared. Doors that open from now on say so to the client.
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// What happens once, when the last sector is cleared: the rest of its monsters go, the clock stops, the hidden event is
    /// decided and each player present has the clear recorded, which tells its client the group of the maze.
    /// </summary>
    private async ValueTask CompleteMazeAsync(SectorBox last)
    {
        if (IsComplete)
        {
            return;
        }

        IsComplete = true;
        MazeState = MazeState.None;

        // The hidden event is decided before the rewards of the clear, which carry its item.
        DecideHiddenEvent();
        PayUnityPoints();

        Completed?.Invoke();

        foreach (var player in Present())
        {
            if (Closed)
            {
                return;
            }

            if (!Ecs.IsAlive(player))
            {
                continue;
            }

            await reports.MazeClearedAsync(
                player,
                info,
                PlayedSeconds,
                (int)Ecs.Get<PersonInfoComponent>(player).Hero,
                Ecs.TryGet<LevelComponent>(player, out var level) ? level.Level : PersonModuleDefines.DefaultLevel);

            if (HiddenEventMet && !Closed && Ecs.IsAlive(player))
            {
                await reports.HiddenEventMetAsync(player);
            }
        }

        if (Closed)
        {
            return;
        }

        await DestroyAsync(creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<NonPlayableCreatureComponent>(e).Sector == last.Id).ToArray(), false);
    }
}
