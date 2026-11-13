using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Lua;
using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Tower;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    private async ValueTask CheckSectorAsync(int sector)
    {
        var kills = creatureManager.GetKillProgress(sector);

        if (kills.IsCleared(creatureManager.CountBoxesToCome(sector)))
        {
            await CompleteSectorAsync(sector);
        }

        if (!Closed)
        {
            await CheckSpawnStepAsync(sector);
        }
    }

    private void WakeSectorBehindGate(int gate, bool open)
    {
        creatureManager.SetGate(gate, open);

        if (open && Batch.EventBox.ServerGates.Find(gate) is { NextSector: > 0 } box)
        {
            SpawnSector(box.NextSector);
        }
    }

    private (int Identifier, NonPlayableCreatureComponent Place, System.Numerics.Vector3 Position) RemoveDead(Entity target)
    {
        var identifier = Ecs.Get<CreatureComponent>(target).Identifier;
        var place = Ecs.Get<NonPlayableCreatureComponent>(target);
        var position = Ecs.Get<WorldComponent>(target).Position;

        creatureManager.Broadcast(new MonsterDieResponse { Actor = Ecs.Get<ActorComponent>(target) });

        creatureManager.Remove(target);

        return (identifier, place, position);
    }

    /// <summary>
    /// A cleared sector tells its script, then the client, and opens its way out. What lies behind comes to life.
    /// </summary>
    private async ValueTask CompleteSectorAsync(int sector)
    {
        var box = Batch.EventBox.Sectors.Find(sector);

        // Sectors of other kinds are finished by their script, or by the quests of the player.
        if (box is null || !Progress.ClearSector(sector))
        {
            return;
        }

        await FinishSectorAsync(box);
    }

    private async ValueTask FinishSectorAsync(SectorBox box)
    {
        await State.OnCompleteSector(box.Id, this);

        if (Closed)
        {
            return;
        }

        creatureManager.Broadcast(new MazeClearSectorResponse { Sector = box.Id, Status = true });

        foreach (var player in Present())
        {
            if (Ecs.IsAlive(player))
            {
                await reports.SectorClearedAsync(player, info.Id, box.Id);
            }

            if (Closed)
            {
                return;
            }
        }

        // The way out of the last sector leads out of the maze: the maze is won before the door says so.
        if (IsLastSector(box))
        {
            await CompleteMazeAsync(box);
        }

        OpenExit(box);
    }

    /// <summary>
    /// The way out of a sector is a door or a portal, and it opens once the sector is cleared.
    /// </summary>
    private void OpenExit(SectorBox box)
    {
        if (box.Exit == 0)
        {
            return;
        }

        if (box.ExitType == SectorExitType.Gate)
        {
            SetGateFlag(box.Exit, true);
        }
        else
        {
            SetPotalFlag(box.Exit, true);
        }
    }

    /// <summary>
    /// The last sector is the one with the boss, or the one whose portal leads out of the maze. The maze is won
    /// when it is cleared, even if a calm sector with the way out lies behind it. A stage of the infinite tower is one sector,
    /// whose portal only leads on within the batch the stages share: clearing it wins the stage.
    /// </summary>
    private bool IsLastSector(SectorBox box)
    {
        if (box.Type == SectorType.Boss || info.Type == InfiniteTower.TowerMazeKind)
        {
            return true;
        }

        return box.ExitType == SectorExitType.Portal
            && Batch.EventBox.Portals.Find(box.Exit) is { JumpType: not JumpType.JumpPos };
    }

    /// <summary>
    /// Everything that lives in the sector dies at once and without a reward.
    /// </summary>
    [LuaMember]
    public void AllDestroySectorMonster(int sector)
    {
        Later(0f, () => DestroyAsync(MonstersOf(sector), false));
    }

    /// <summary>
    /// Makes the monsters of a table die, wherever they are. Monsters are the type 2, townsfolk the type 1.
    /// </summary>
    [LuaMember]
    public void ExecuteDestroy(int type, string table, string animation, bool suicide = true)
    {
        if (type != 2 || !int.TryParse(table, out var identifier))
        {
            NotDone("ExecuteDestroy");
            Logger.IgnoredExecuteDestroy(type, table, animation, suicide);
            return;
        }

        Later(0f, () => DestroyAsync(
            creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<CreatureComponent>(e).Identifier == identifier).ToArray(),
            false));
    }

    /// <summary>
    /// A sector the script clears is finished like one that was cleared by the fight.
    /// </summary>
    [LuaMember]
    public void SectorClear(int sector)
    {
        Later(0f, () => CompleteSectorAsync(sector));
    }

    [LuaMember]
    public void SetGateFlag(int gate, bool open)
    {
        if (Progress.SetGate(gate, open))
        {
            var next = Batch.EventBox.ServerGates.Find(gate)?.NextSector ?? 0;

            creatureManager.Broadcast(new WorldGateEnableResponse { Gate = gate, Open = open, IsMazeComplete = IsComplete, NextSector = next });

            WakeSectorBehindGate(gate, open);
        }
    }

    [LuaMember]
    public void SetGateFlagNoSend(int gate, bool open)
    {
        if (Progress.SetGate(gate, open))
        {
            WakeSectorBehindGate(gate, open);
        }
    }

    /// <summary>
    /// The sector the first player of the maze stands in.
    /// </summary>
    private int CurrentSector => _members.Count == 0
        ? 0
        : Batch.EventBox.Sectors.FirstOrDefault(e => Contains(e, Ecs.Get<WorldComponent>(_members[0].Player).Position))?.Id ?? 0;

    private static bool Contains(SectorBox box, Vector3 position)
    {
        return position.X >= Math.Min(box.PosTopLeft.X, box.PosBottomRight.X) &&
               position.X <= Math.Max(box.PosTopLeft.X, box.PosBottomRight.X) &&
               position.Y >= Math.Min(box.PosTopLeft.Y, box.PosBottomRight.Y) &&
               position.Y <= Math.Max(box.PosTopLeft.Y, box.PosBottomRight.Y);
    }

    private Entity[] MonstersOf(int sector)
    {
        return creatureManager.FindMonsters((_, _) => true).Where(e => Ecs.Get<NonPlayableCreatureComponent>(e).Sector == sector).ToArray();
    }

    /// <summary>
    /// Kills what is given: the client sees them die, and the sector counts them.
    /// </summary>
    /// <param name="scripts">Whether the script hears of each death.</param>
    private async ValueTask DestroyAsync(Entity[] targets, bool scripts)
    {
        var dead = targets.Where(Ecs.IsAlive).Select(RemoveDead).ToArray();

        foreach (var (identifier, place, _) in dead)
        {
            if (Closed)
            {
                return;
            }

            if (scripts)
            {
                await State.OnDie(identifier, place.SpawnBox, this);
            }
        }

        foreach (var sector in dead.Select(e => e.Place.Sector).Distinct())
        {
            if (Closed)
            {
                return;
            }

            await CheckSectorAsync(sector);
        }
    }

    /// <summary>
    /// Kills what a sector holds and finishes it, for a command that wants a sector done at once.
    /// </summary>
    [LuaIgnoreMember]
    public async ValueTask ClearSectorAsync(int sector)
    {
        if (sector == 0)
        {
            sector = CurrentSector;
        }

        await DestroyAsync(MonstersOf(sector), true);

        if (!Closed)
        {
            await CompleteSectorAsync(sector);
        }
    }
}
