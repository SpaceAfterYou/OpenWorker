using System.Collections.ObjectModel;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// How much stronger a monster is for the party that plays its maze. The strength of a monster follows from its group
/// (a column of the monster table of the server) and the number of members the maze counts: one to four, and a monster that
/// is not of the plain kind counts as a party of one. The rows multiply the life, the attacks and the defences of the monster,
/// and give its super armor a gauge of a share of its life that takes its time to come back.
/// </summary>
/// <remarks>
/// The rates apply to a player alone as well: a group says what its monsters are for a party of one. A monster whose group has
/// no row for the number of members keeps the strength its own table gives.
/// </remarks>
public sealed class PartyRevision(ReadOnlyCollection<MonsterReviseGroupRow> monsters, ReadOnlyCollection<PartyReviseRatesRow> rates)
{
    /// <summary>
    /// The most members the strength counts.
    /// </summary>
    public const int MostMembers = 4;

    private const float Percent = 0.01f;

    private readonly RowIndex<uint, MonsterReviseGroupRow> _monsters = monsters.IndexBy(e => e.Id);
    private readonly RowIndex<(uint Group, byte Members), PartyReviseRatesRow> _rates = rates.IndexBy(e => (e.Group, e.MemberCount));

    /// <summary>
    /// Whether the tables the strength comes from are there.
    /// </summary>
    public bool IsAvailable => _monsters.Count > 0 && _rates.Count > 0;

    /// <summary>
    /// Whether the strength of the monster follows the party when it changes: it is of the plain kind, which the monsters
    /// that fight beside a boss or follow another are not.
    /// </summary>
    public bool IsRevised(int monster)
    {
        return monster >= 0 && _monsters.TryGet((uint)monster, out var table) && table.Type == 0;
    }

    /// <summary>
    /// The row that says how strong the monster is for the number of members, if the tables have one.
    /// </summary>
    public bool TryGet(int monster, int members, out PartyReviseRatesRow row)
    {
        row = default;

        if (monster < 0 || !_monsters.TryGet((uint)monster, out var table))
        {
            return false;
        }

        return _rates.TryGet((table.PartyRevise, (byte)Members(table, members)), out row);
    }

    /// <summary>
    /// The number of members the strength of the monster is counted for.
    /// </summary>
    private static int Members(in MonsterReviseGroupRow monster, int members)
    {
        return monster.Type != 0 ? 1 : Math.Clamp(members, 1, MostMembers);
    }

    /// <summary>
    /// The monster as strong as it is for the number of members: its life, attacks and defences multiplied by the rates of
    /// its group, and the super armor sized to its life when it has one. The rates apply to the whole number the stat
    /// holds, not to its fraction.
    /// </summary>
    public MonsterCombatComponent Apply(in MonsterCombatComponent combat, int monster, int members)
    {
        if (!TryGet(monster, members, out var row))
        {
            return combat;
        }

        var stats = combat.Stats;

        stats = stats with
        {
            MaxHealth = Multiply(stats.MaxHealth, row.HealthRate),
            Physical = Scale(stats.Physical, row),
            Magical = Scale(stats.Magical, row)
        };

        var result = combat with { Stats = stats };

        if (combat.Defense == DefenseType.SuperArmor)
        {
            var gauge = stats.MaxHealth * row.SuperArmorGauge * Percent;

            result = result with
            {
                SuperArmor = gauge,
                MaxSuperArmor = gauge,
                SuperArmorRecovery = row.SuperArmorRecoverTime
            };
        }

        return result;
    }

    private static DamageStats Scale(in DamageStats stats, in PartyReviseRatesRow row)
    {
        return stats with
        {
            AttackMin = Multiply(stats.AttackMin, row.AttackRate),
            AttackMax = Multiply(stats.AttackMax, row.AttackRate),
            Defense = Multiply(stats.Defense, row.DefenseRate)
        };
    }

    private static float Multiply(float value, float rate)
    {
        return (int)value * rate;
    }
}
