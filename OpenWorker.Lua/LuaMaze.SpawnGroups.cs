using Lua;
using Microsoft.Extensions.Logging;
using OpenWorker.Batch.Entities;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Lua;

/// <summary>
/// The spawn boxes of a map that share a group: a script calls them out together, and hears when the last monster of the
/// group has fallen (a step of a time attack is won that way).
/// </summary>
public partial class LuaMaze
{
    private ILookup<int, MonsterSpawnBox>? _spawnGroups;

    /// <summary>
    /// The groups whose monsters have all fallen since the script last called them out; each falls once.
    /// </summary>
    private readonly HashSet<int> _fallenGroups = [];

    /// <summary>
    /// The boxes of each group: those in the sector of the first box of the group.
    /// </summary>
    private ILookup<int, MonsterSpawnBox> SpawnGroups => _spawnGroups ??= Batch.EventBox.MonsterSpawns
        .Where(e => e.Group > 0)
        .GroupBy(e => e.Group)
        .SelectMany(e => e.Where(box => box.Sector == e.First().Sector))
        .ToLookup(e => e.Group);

    [LuaMember("ExcutEventSpawnGroupLua")]
    public void ExecuteEventSpawnGroupLua(int group)
    {
        var boxes = SpawnGroups[group].ToArray();

        LogSpawnGroup(Logger, group, boxes.Length);

        _fallenGroups.Remove(group);

        foreach (var box in boxes)
        {
            creatureManager.Create(box.Id);
        }
    }

    /// <summary>
    /// A monster of the box has fallen: its group, when no creature of that group is left standing, else zero. A group
    /// falls once until the script calls it out again.
    /// </summary>
    internal int TakeFallenGroup(int box)
    {
        if (Batch.EventBox.MonsterSpawns.Find(box) is not { Group: > 0 } spawn || _fallenGroups.Contains(spawn.Group))
        {
            return 0;
        }

        foreach (var creature in creatureManager.Living)
        {
            if (Ecs.IsAlive(creature) && Ecs.TryGet<NonPlayableCreatureComponent>(creature, out var place) &&
                Batch.EventBox.MonsterSpawns.Find(place.SpawnBox) is { } other && other.Group == spawn.Group)
            {
                return 0;
            }
        }

        _fallenGroups.Add(spawn.Group);

        return spawn.Group;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script call ExcutEventSpawnGroupLua: group {Group}, {Boxes} boxes")]
    private static partial void LogSpawnGroup(ILogger logger, int group, int boxes);
}
