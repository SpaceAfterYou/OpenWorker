using System.Diagnostics;
using Arch.Core.Extensions;
using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Gameplay.Messages.Response.Person.Components;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// A group of mazes has four episodes.
    /// </summary>
    private const int MaxEpisode = 4;

    /// <summary>
    /// The maze is won: its last sector has been cleared. Doors that open from now on say so to the client.
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// The client learns which maze of its group is cleared, and that the next episode is open. The mazes cleared
    /// before are not kept anywhere yet, so only this one is told.
    /// </summary>
    private void SendClearInfo()
    {
        var group = new MazeClearValue(info.Group, (byte)Math.Min(info.Episode + 1, MaxEpisode), [(info.Id, 1)]);

        foreach (var member in _members)
        {
            member.Player.Get<ServerSessionComponent>().Send(new MazeClearInfoResponse { Groups = [group], Person = Ecs.Get<ActorComponent>(member.Player), IsReset = false });
        }
    }

    /// <summary>
    /// What happens once, when the last sector is cleared: the rest of its monsters go, the clock stops and the
    /// client is told that the maze is won.
    /// </summary>
    private async ValueTask CompleteMazeAsync(SectorBox last)
    {
        if (IsComplete)
        {
            return;
        }

        IsComplete = true;
        MazeState = MazeState.None;

        SendClearInfo();

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
                (int)Stopwatch.GetElapsedTime(StartedAt).TotalSeconds,
                (int)Ecs.Get<PersonInfoComponent>(player).Hero,
                Ecs.TryGet<LevelComponent>(player, out var level) ? level.Level : PersonModuleDefines.DefaultLevel);
        }

        if (Closed)
        {
            return;
        }

        await DestroyAsync(creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<NonPlayableCreatureComponent>(e).Sector == last.Id).ToArray(), false);
    }
}
