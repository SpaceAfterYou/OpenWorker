using Lua;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// Spawns what a sector starts with. Boxes that wait for a signal are started by the script.
    /// </summary>
    [LuaIgnoreMember]
    public void SpawnSector(int sector)
    {
        var kills = creatureManager.GetKillProgress(sector);

        kills.MarkCurrentStepSpawned();

        foreach (var box in Batch.EventBox.SpawnsOf(sector).Where(e => e.CreationCondition == CreationConditionType.Immediate && e.Step == kills.Step + 1))
        {
            creatureManager.SpawnOnce(box.Id);
        }
    }

    /// <summary>
    /// Enough dead open the next wave of a sector, unless the script holds it back.
    /// </summary>
    private async ValueTask CheckSpawnStepAsync(int sector)
    {
        var kills = creatureManager.GetKillProgress(sector);

        if (kills.Stopped)
        {
            return;
        }

        if (!kills.IsCurrentStepSpawned)
        {
            SpawnSector(sector);

            return;
        }

        if (!kills.TryOpenNextStep())
        {
            return;
        }

        await State.OnCompleteSpawnStepCondition(sector, kills.Step, this);

        if (!Closed && !kills.Stopped)
        {
            SpawnSector(sector);
        }
    }

    [LuaMember("ExcutEventSpawnGroupLua")]
    public void ExecuteEventSpawnGroupLua(int group)
    {
        NotDone("ExcutEventSpawnGroupLua");
        Logger.IgnoredExecuteEventSpawnGroup(group);
    }

    [LuaMember("ExcutEventSpawnLua")]
    public void ExecuteEventSpawnLua(int box)
    {
        Logger.CallExecuteEventSpawn(box);

        creatureManager.Create(box);
    }

    [LuaMember]
    public void SetSectorStepStop(int sector, bool stop)
    {
        var kills = creatureManager.GetKillProgress(sector);

        if (kills.Stopped == stop)
        {
            return;
        }

        kills.Stopped = stop;

        if (!stop)
        {
            Later(0f, () => CheckSpawnStepAsync(sector));
        }
    }

    /// <summary>
    /// No more monsters come to a sector. The sector itself is the current one when the number is zero.
    /// </summary>
    [LuaMember]
    public void TerminateSpawn(int sector)
    {
        if (sector == 0)
        {
            sector = CurrentSector;
        }

        creatureManager.GetKillProgress(sector).Stopped = true;

        foreach (var box in Batch.EventBox.SpawnsOf(sector))
        {
            creatureManager.Terminate(box.Id);
        }

        Later(0f, () => CheckSectorAsync(sector));
    }

    /// <summary>
    /// A box that has not appeared yet never will. What has appeared stays.
    /// </summary>
    [LuaMember]
    public void TerminateSpawnBox(int box)
    {
        creatureManager.Terminate(box);

        if (Batch.EventBox.MonsterSpawns.Find(box) is { } spawn)
        {
            Later(0f, () => CheckSectorAsync(spawn.Sector));
        }
    }
}
