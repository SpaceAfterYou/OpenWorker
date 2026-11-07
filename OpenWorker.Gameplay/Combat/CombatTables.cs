using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// What a monster is made of, for the time it lives.
/// </summary>
public readonly record struct MonsterCombatComponent
{
    public CombatStats Stats { get; init; }
    public DefenseType Defense { get; init; }
    public float BodyRadius { get; init; }
    public float BodyHeight { get; init; }

    /// <summary>
    /// Takes one point per hit, however strong the hit is.
    /// </summary>
    public bool CountsHits { get; init; }

    public long LastCountedHit { get; init; }

    /// <summary>
    /// What is left of the super armor of the monster. It takes hits without flinching while there is some.
    /// </summary>
    public float SuperArmor { get; init; }

    public float MaxSuperArmor { get; init; }

    /// <summary>
    /// When a broken super armor comes back, in milliseconds of the clock of the process. Zero while it holds.
    /// </summary>
    public long SuperArmorBrokenUntil { get; init; }

    /// <summary>
    /// Has to die for its sector to be finished.
    /// </summary>
    public bool HoldsSector { get; init; }
}

public sealed class CombatTables(
    ReadOnlyCollection<MonsterRow> monsters,
    ReadOnlyCollection<StatusRow> statuses,
    ReadOnlyCollection<SkillRow> skills,
    ReadOnlyCollection<CharacterInfoRow> characters,
    ItemCatalog? items = null)
{
    private const int HeroRowStep = 1000;

    private const byte PlainMonster = 0;
    private const byte EquippedMonster = 3;
    private const byte BrokenPartsMonster = 11;
    private const byte HitCountingMonster = 14;

    private const float DefaultBodyRadius = 50f;
    private const float DefaultBodyHeight = 200f;

    private Dictionary<int, MonsterRow> Monsters { get; } = monsters.DistinctBy(e => e.Id).ToDictionary(e => e.Id);
    private Dictionary<short, StatusRow> Statuses { get; } = statuses.DistinctBy(e => e.Id).ToDictionary(e => e.Id);
    private Dictionary<int, SkillRow> Skills { get; } = skills.DistinctBy(e => e.Id).ToDictionary(e => e.Id);
    private Dictionary<(Hero Hero, byte Level), CombatStats> People { get; } = [];
    private Dictionary<int, MonsterCombatComponent> MonsterStats { get; } = [];

    public MonsterCombatComponent GetMonster(int identifier)
    {
        if (MonsterStats.TryGetValue(identifier, out var result))
        {
            return result;
        }

        result = Monsters.TryGetValue(identifier, out var row) ? Create(row) : new MonsterCombatComponent
        {
            Stats = new CombatStats { Level = 1, MaxHealth = 1 },
            BodyRadius = DefaultBodyRadius,
            BodyHeight = DefaultBodyHeight
        };

        MonsterStats.Add(identifier, result);

        return result;
    }

    public CombatStats GetPerson(Hero hero, byte level)
    {
        if (People.TryGetValue((hero, level), out var result))
        {
            return result;
        }

        var character = characters.FirstOrDefault(e => e.Id == (short)((int)hero * HeroRowStep));

        result = Statuses.TryGetValue((short)character.Character_Status, out var status)
            ? CombatStatCalculator.Calculate(status, level, character.Character_Status_Type)
            : new CombatStats { Level = level };

        People.Add((hero, level), result);

        return result;
    }

    /// <summary>
    /// A class at a level with the stats that its gear adds.
    /// </summary>
    public CombatStats GetPerson(Hero hero, byte level, in StatBonuses gear)
    {
        if (gear == default)
        {
            return GetPerson(hero, level);
        }

        var character = characters.FirstOrDefault(e => e.Id == (short)((int)hero * HeroRowStep));

        return Statuses.TryGetValue((short)character.Character_Status, out var status)
            ? CombatStatCalculator.Calculate(status, level, character.Character_Status_Type, gear)
            : new CombatStats { Level = level };
    }

    /// <summary>
    /// The stats of a player as they are now: the level it has reached and what it wears.
    /// </summary>
    public CombatStats GetPlayer(World world, Entity player)
    {
        var hero = world.Get<PersonInfoComponent>(player).Hero;
        var level = world.Get<RankComponent>(player).Level;

        var gear = items is null || !world.Has<InventoryComponent>(player)
            ? default
            : EquipmentStats.Calculate(world.Get<InventoryComponent>(player).Value, items, hero);

        return GetPerson(hero, level, gear);
    }

    public SkillDamage GetSkill(int identifier)
    {
        return Skills.TryGetValue(identifier, out var row) ? SkillDamage.From(row) : default;
    }

    /// <summary>
    /// Whether a monster kind keeps a sector open until it is dead.
    /// </summary>
    public bool HoldsSector(int identifier)
    {
        return Monsters.TryGetValue(identifier, out var row) && HoldsSector(row);
    }

    private MonsterCombatComponent Create(in MonsterRow row)
    {
        var stats = Statuses.TryGetValue((short)row.StatusId, out var status)
            ? CombatStatCalculator.Calculate(status, (byte)row.Level, (byte)row.StatusType, StatBonuses.From(row))
            : new CombatStats { Level = (byte)row.Level, MaxHealth = Math.Max(1, row.AddHealth) };

        return new MonsterCombatComponent
        {
            Stats = stats,
            Defense = (DefenseType)row.DefenseType,
            BodyRadius = row.CollisionRadius > 0 ? row.CollisionRadius : DefaultBodyRadius,
            BodyHeight = row.CollisionHeight > 0 ? row.CollisionHeight : DefaultBodyHeight,
            CountsHits = row.Type == HitCountingMonster,
            HoldsSector = HoldsSector(row)
        };
    }

    private static bool HoldsSector(in MonsterRow row)
    {
        return row.Type is PlainMonster or EquippedMonster or BrokenPartsMonster && row.ClearConditionType == 0;
    }
}
