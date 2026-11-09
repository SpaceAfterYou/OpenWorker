using System.Collections.ObjectModel;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Helpers;

/// <summary>
/// The stats of a helper. A helper has no level of its own: what it has by itself follows the level of its owner, from the
/// status row of its monster and the flat additions of that monster row (the level of the monster row is not used). What it
/// wears and the orb of a friend add physical attack and defense on top.
/// </summary>
public sealed class HelperStats
{
    private const byte PhysicalAttackMin = (byte)STAT_TYPE.STAT_PATK_MIN;
    private const byte PhysicalAttackMax = (byte)STAT_TYPE.STAT_PATK_MAX;
    private const byte PhysicalDefense = (byte)STAT_TYPE.STAT_PDEF;

    /// <summary>
    /// The orb that adds to the attack; the other one adds to the defense.
    /// </summary>
    public const byte AttackOrb = 0;

    /// <summary>
    /// The orb that adds to the defense.
    /// </summary>
    public const byte DefenseOrb = 1;

    private readonly RowIndex<int, MonsterRow> _monsters;
    private readonly RowIndex<short, StatusRow> _statuses;
    private readonly ItemCatalog? _items;
    private readonly TimeProvider _time;

    public HelperStats(
        HelperCatalog catalog,
        ReadOnlyCollection<MonsterRow> monsters,
        ReadOnlyCollection<StatusRow> statuses,
        ItemCatalog? items = null,
        TimeProvider? time = null)
    {
        _monsters = monsters.Where(e => catalog.Contains(e.Id)).IndexBy(e => e.Id);
        _statuses = statuses.IndexBy(e => e.Id);
        _items = items;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>
    /// What a helper has by itself when its owner is at this level, or nothing for a helper the tables do not know.
    /// </summary>
    public HelperStatValue[] Origin(int helper, byte level)
    {
        return _monsters.TryGet(helper, out var monster) && _statuses.TryGet((short)monster.StatusId, out var status)
            ? Origin(status, monster.StatusType, StatBonuses.From(monster), level)
            : [];
    }

    /// <summary>
    /// The stats of a character of this status at this level with these flat additions, as the stat numbers the client
    /// keeps for a helper. Zero stats are left out.
    /// </summary>
    public static HelperStatValue[] Origin(in StatusRow status, byte statusType, in StatBonuses additions, byte level)
    {
        return CombatStatCalculator.Calculate(status, level, statusType, additions)
            .ToStatList()
            .Select(e => new HelperStatValue((byte)e.byIndex, e.statValue))
            .ToArray();
    }

    /// <summary>
    /// What a called helper fights with: what it has by itself at its owner's level, and on top of it what its record says
    /// its items and orb add to the attacks and the defense.
    /// </summary>
    public CombatStats Combat(int helper, byte level, IEnumerable<HelperStatValue> additional)
    {
        var stats = _monsters.TryGet(helper, out var monster) && _statuses.TryGet((short)monster.StatusId, out var status)
            ? CombatStatCalculator.Calculate(status, level, monster.StatusType, StatBonuses.From(monster))
            : new CombatStats { Level = level, MaxHealth = 1 };
        var physical = stats.Physical;

        foreach (var (stat, value) in additional)
        {
            physical = stat switch
            {
                PhysicalAttackMin => physical with { AttackMin = physical.AttackMin + value },
                PhysicalAttackMax => physical with { AttackMax = physical.AttackMax + value },
                PhysicalDefense => physical with { Defense = physical.Defense + value },
                _ => physical
            };
        }

        return stats with { Physical = physical };
    }

    /// <summary>
    /// What the items a helper wears and the orb of a friend add to it.
    /// </summary>
    public HelperStatValue[] Additional(IEnumerable<int> worn, in HelperSupportInfoValue orb)
    {
        var items = worn
            .Select(e => _items is not null && _items.TryGet(e, out var row) ? (row.PhysicalAttack, row.PhysicalDefense) : (0, 0));

        return Additional(items, orb, _time.GetUtcNow());
    }

    /// <summary>
    /// The physical attack of every item adds to both attacks, its physical defense to the defense; an orb adds its value to
    /// both attacks or to the defense, while its date is still to come.
    /// </summary>
    public static HelperStatValue[] Additional(IEnumerable<(int Attack, int Defense)> items, in HelperSupportInfoValue orb, DateTimeOffset now)
    {
        float attack = 0;
        float defense = 0;

        foreach (var (itemAttack, itemDefense) in items)
        {
            attack += itemAttack;
            defense += itemDefense;
        }

        if (orb.Friend != 0 && orb.Value > 0 && orb.Date > now)
        {
            switch (orb.SupportType)
            {
                case AttackOrb:
                    attack += orb.Value;
                    break;
                case DefenseOrb:
                    defense += orb.Value;
                    break;
            }
        }

        (byte Stat, float Value)[] stats = [(PhysicalAttackMin, attack), (PhysicalAttackMax, attack), (PhysicalDefense, defense)];

        return stats.Where(e => e.Value != 0).Select(e => new HelperStatValue(e.Stat, e.Value)).ToArray();
    }
}
