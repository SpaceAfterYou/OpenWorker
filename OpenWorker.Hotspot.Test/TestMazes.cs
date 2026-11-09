using System.Collections.ObjectModel;
using Arch.Core;
using Lua;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Batch;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Lua;
using OpenWorker.Lua.Managers;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Mazes for the tests that need one: no tables, and a map with only the boxes a test gives.
/// </summary>
internal static class TestMazes
{
    /// <summary>
    /// A map with no boxes at all, so that every box a client or a script names is unknown.
    /// </summary>
    public const string EmptyMap = "<root><Batchs eventtype=\"EventBox\"/><Batchs eventtype=\"EventPoint\"/></root>";

    public static ReadOnlyCollection<T> None<T>() => new([]);

    /// <summary>
    /// Quests with no episodes: the tests never get to them.
    /// </summary>
    public static QuestManager Quests(World world)
    {
        return new QuestManager(
            world,
            null!,
            QuestCatalog.From([], []),
            null!,
            null!,
            null!,
            null!,
            NullLogger<QuestManager>.Instance);
    }

    public static SimulationClock Clock(TimeProvider? time = null)
    {
        return new SimulationClock(new WorldLoop(NullLogger<WorldLoop>.Instance), NullLogger<SimulationClock>.Instance, time: time);
    }

    public static VBatchFile Map(string map = EmptyMap)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.vbatch");

        File.WriteAllText(path, map);

        try
        {
            return VBatchFile.CreateFromPath(path);
        }

        finally
        {
            File.Delete(path);
        }
    }

    public static LuaMaze Create(World world, Entity player, ILogger<LuaMaze>? logger = null, SimulationClock? clock = null, LuaState? state = null, string map = EmptyMap)
    {
        var batch = Map(map);
        var tables = new CombatTables(None<MonsterRow>(), None<StatusRow>(), None<SkillRow>(), None<CharacterInfoRow>());

        return new LuaMaze(
            state ?? LuaState.Create(),
            world,
            player,
            new LuaCreatureManager(world, batch, tables),
            new BuffManager(world, None<BuffRow>()),
            Quests(world),
            batch,
            None<InteractionObjectRow>().IndexBy(e => e.Id),
            new SkillActionProvider(None<SkillRow>(), NullLogger<SkillActionProvider>.Instance),
            tables,
            default,
            // The tests do nothing a maze reports.
            null!,
            clock ?? Clock(),
            logger ?? NullLogger<LuaMaze>.Instance);
    }
}
