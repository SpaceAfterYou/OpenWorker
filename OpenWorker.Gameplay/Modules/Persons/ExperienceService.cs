using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.Gameplay.Modules.Persons;

/// <summary>
/// Gives experience to players and raises their level. A level asks for as much experience as its row of the table says,
/// what is left over after a level counts toward the next one.
/// </summary>
public sealed class ExperienceService
{
    private const int LastRank = 6;

    private readonly float _rate;
    private readonly World _world;
    private readonly InventoryService _inventory;
    private readonly PlayerStatsService? _stats;
    private readonly Dictionary<int, int> _need;
    private readonly Dictionary<int, MonsterExpRow> _monsterExp;
    private readonly Dictionary<int, MonsterRow> _monsters;
    private readonly Dictionary<int, float> _rewards;
    private readonly Dictionary<int, float> _intervals;

    public ExperienceService(
        World world,
        InventoryService inventory,
        ReadOnlyCollection<LevelupPointRow> levels,
        ReadOnlyCollection<MonsterExpRow> monsterExp,
        ReadOnlyCollection<PartyExpMobRow> intervals,
        ReadOnlyCollection<MonsterRewardRow> rewards,
        ReadOnlyCollection<MonsterRow> monsters,
        PlayerStatsService? stats = null,
        IOptions<RewardsOptions>? options = null)
    {
        _rate = options?.Value.ExpRate ?? 1f;
        _world = world;
        _inventory = inventory;
        _stats = stats;
        _need = levels.GroupBy(e => (int)e.Id).ToDictionary(e => e.Key, e => e.First().Experience);
        _monsterExp = monsterExp.GroupBy(e => e.Id).ToDictionary(e => (int)e.Key, e => e.First());
        _monsters = monsters.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
        _rewards = rewards.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First().ExpRate);
        _intervals = intervals.GroupBy(e => (int)e.Interval).ToDictionary(e => e.Key, e => e.First().Rate);
    }

    /// <summary>
    /// The highest level the table lets a player reach.
    /// </summary>
    public int MaxLevel => _need.Count == 0 ? 1 : _need.Keys.Max();

    /// <summary>
    /// How much experience a level asks for to get to the next one, 0 for the last level.
    /// </summary>
    public int Need(int level)
    {
        return level < MaxLevel && _need.TryGetValue(level, out var need) ? need : 0;
    }

    /// <summary>
    /// What killing a monster is worth to a player of the same level.
    /// </summary>
    public int ForMonster(int monster)
    {
        if (!_monsters.TryGetValue(monster, out var row) || !_monsterExp.TryGetValue(row.Level, out var table) || row.Rank is < 0 or > LastRank)
        {
            return 0;
        }

        // The client table does not say how much a monster is worth next to others of its rank. Those that hold a sector
        // are worth the whole of it, the rest, such as objects that can be broken, are worth nothing.
        var rate = _rewards.TryGetValue(monster, out var share) ? share : _rewards.Count == 0 && row.Type is 0 or 3 or 11 ? 1f : 0f;

        return (int)(table.ByRank[row.Rank] * rate);
    }

    /// <summary>
    /// Gives the player the experience for a monster of this level, less of it when the player has outgrown the monster.
    /// Returns how much it was.
    /// </summary>
    public int GiveForMonster(Entity player, int monster)
    {
        if (!_monsters.TryGetValue(monster, out var row))
        {
            return 0;
        }

        var experience = (double)ForMonster(monster);

        if (experience <= 0)
        {
            return 0;
        }

        var interval = Math.Clamp(_world.Get<LevelComponent>(player).Level - row.Level, -100, 100);

        if (_intervals.TryGetValue(interval, out var rate))
        {
            experience *= rate;
        }

        var given = (int)Math.Ceiling(experience * _rate);

        Add(player, given);

        return given;
    }

    /// <summary>
    /// Adds experience, raising the level as many times as it pays for. The player is told how much it gained.
    /// </summary>
    public void Add(Entity player, int experience, int bonus = 0)
    {
        if (experience <= 0 || !_world.IsAlive(player))
        {
            return;
        }

        var rank = _world.Get<LevelComponent>(player);

        if (rank.Level >= MaxLevel)
        {
            return;
        }

        var level = (int)rank.Level;
        var total = (long)rank.Experience + experience;

        while (level < MaxLevel && Need(level) is var need && need > 0 && total >= need)
        {
            total -= need;
            level++;
        }

        if (level >= MaxLevel)
        {
            total = 0;
        }

        _world.Set(player, new LevelComponent((byte)level, (int)total));
        _world.MarkDirty(player, PersonSections.Level);

        if (_world.Has<ServerSessionComponent>(player))
        {
            _world.Get<ServerSessionComponent>(player).Send(new PersonExpResponse { Value = (int)total, Additional = experience, Bonus = bonus });
        }

        if (level != rank.Level)
        {
            LevelChanged(player, (byte)level);
        }
    }

    private void LevelChanged(Entity player, byte level)
    {
        if (_world.Has<ServerSessionComponent>(player))
        {
            _world.Get<ServerSessionComponent>(player).Send(new PersonLevelResponse { Actor = _world.Get<ActorComponent>(player), Value = level });
        }

        // The class gets stronger with every level.
        _stats?.Send(player);
    }
}
