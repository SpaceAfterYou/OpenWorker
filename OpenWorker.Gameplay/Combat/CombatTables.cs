using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Boosters.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Res;
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

    /// <summary>
    /// The game time of the last hit that took a point; none before the first.
    /// </summary>
    public TimeSpan? LastCountedHit { get; init; }

    /// <summary>
    /// What is left of the super armor of the monster. It takes hits without flinching while there is some.
    /// </summary>
    public float SuperArmor { get; init; }

    public float MaxSuperArmor { get; init; }

    /// <summary>
    /// The game time a broken super armor comes back at; none while it holds.
    /// </summary>
    public TimeSpan? SuperArmorBrokenUntil { get; init; }

    /// <summary>
    /// How long a broken super armor takes to come back, in milliseconds; zero for the time every monster has by default.
    /// </summary>
    public long SuperArmorRecovery { get; init; }

    /// <summary>
    /// Has to die for its sector to be finished.
    /// </summary>
    public bool HoldsSector { get; init; }

    /// <summary>
    /// The rank of the monster in its table: 3, 4 and 5 are bosses.
    /// </summary>
    public byte Rank { get; init; }

    /// <summary>
    /// Which side of the world the monster belongs to and whether it shoots; the special effects of gear work on both.
    /// </summary>
    public byte Faction { get; init; }

    public byte StatusType { get; init; }

    public bool IsBoss => Rank is 3 or 4 or 5;

    /// <summary>
    /// How heavy the monster is: a heavy attacker throws a light monster farther.
    /// </summary>
    public byte WeightRank { get; init; }

    /// <summary>
    /// What the monster cannot be thrown by.
    /// </summary>
    public ReactionLimit ReactionLimit { get; init; }

    /// <summary>
    /// The monster shows an animation when its super armor breaks and stands helpless through it.
    /// </summary>
    public bool ArmorBreakMotion { get; init; }

    /// <summary>
    /// How the monster stands: 2 and 3 are fixed in their place for good, 1 waits for a fight.
    /// </summary>
    public byte StandType { get; init; }

    /// <summary>
    /// A defensive weapon: a player gets on it and fires it. Nothing can hurt it.
    /// </summary>
    public bool IsDefensiveWeapon { get; init; }
}

public sealed class CombatTables(
    ReadOnlyCollection<MonsterRow> monsters,
    ReadOnlyCollection<StatusRow> statuses,
    ReadOnlyCollection<SkillRow> skills,
    ReadOnlyCollection<CharacterInfoRow> characters,
    TitleCatalog? titles = null,
    ItemCatalog? items = null,
    ReadOnlyCollection<StatusSoulGaugeRow>? soulGauges = null,
    GearStats? gear = null,
    BoosterCatalog? boosters = null)
{
    private static readonly StatSheet Nothing = new();

    private readonly GearStats? _gear = gear ?? (items is null ? null : new GearStats(items));

    private const int HeroRowStep = 1000;

    private const byte PlainMonster = 0;
    private const byte EquippedMonster = 3;
    private const byte BrokenPartsMonster = 11;
    private const byte HitCountingMonster = 14;
    private const byte DefensiveWeaponMonster = 5;
    private const byte ArmorBreakMotionType = 1;

    private const float DefaultBodyRadius = 50f;
    private const float DefaultBodyHeight = 200f;

    private Dictionary<int, MonsterRow> Monsters { get; } = monsters.DistinctBy(e => e.Id).ToDictionary(e => e.Id);
    private Dictionary<short, StatusRow> Statuses { get; } = statuses.DistinctBy(e => e.Id).ToDictionary(e => e.Id);
    private Dictionary<int, SkillRow> Skills { get; } = skills.DistinctBy(e => e.Id).ToDictionary(e => e.Id);
    private readonly RowIndex<short, CharacterInfoRow> _characters = characters.IndexBy(e => e.Id);
    private Dictionary<(Hero Hero, byte Level), CombatStats> People { get; } = [];
    private Dictionary<int, MonsterCombatComponent> MonsterStats { get; } = [];
    private readonly RowIndex<short, StatusSoulGaugeRow> _soulGauges = (soulGauges ?? new ReadOnlyCollection<StatusSoulGaugeRow>([])).IndexBy(e => e.Id);

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

    /// <summary>
    /// What a hero is at a level with nothing added: what the client calls its origin stats.
    /// </summary>
    public CombatStats GetPerson(Hero hero, byte level)
    {
        if (People.TryGetValue((hero, level), out var result))
        {
            return result;
        }

        result = GetPerson(hero, level, Nothing);

        People.Add((hero, level), result);

        return result;
    }

    /// <summary>
    /// What a character is capable of with the titles it has chosen for its abilities.
    /// </summary>
    public CombatStats GetPerson(Hero hero, byte level, in TitleComponent title)
    {
        var sheet = new StatSheet();

        AddTitles(sheet, title, hero);

        return sheet.IsEmpty ? GetPerson(hero, level) : GetPerson(hero, level, sheet);
    }

    /// <summary>
    /// The stats of a hero at a level with what is added to them: the one place the stats of a player are worked out.
    /// </summary>
    public CombatStats GetPerson(Hero hero, byte level, StatSheet sheet)
    {
        var character = _characters.GetOrDefault((short)((int)hero * HeroRowStep));

        // A hero without a status (a test with no tables) has nothing but what is added.
        Statuses.TryGetValue((short)character.Character_Status, out var status);

        var stats = CombatStatCalculator.Calculate(status, level, character.Character_Status_Type, sheet);

        return _soulGauges.TryGet(status.Id, out var row) && status.Id != 0
            ? CombatStatCalculator.WithSoulGauge(stats, hero, row.SoulGauge, sheet)
            : stats;
    }

    /// <summary>
    /// The stats of a player as they are now: the level it has reached, what it wears, the titles it has chosen and the buffs
    /// it carries.
    /// </summary>
    public CombatStats GetPlayer(World world, Entity player)
    {
        var hero = world.Get<PersonInfoComponent>(player).Hero;
        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : PersonModuleDefines.DefaultLevel;

        return GetPerson(hero, level, SheetOf(world, player, hero));
    }

    /// <summary>
    /// The stats of a player with nothing added: its hero at the level it has reached.
    /// </summary>
    public CombatStats GetOrigin(World world, Entity player)
    {
        var hero = world.Get<PersonInfoComponent>(player).Hero;
        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : PersonModuleDefines.DefaultLevel;

        return GetPerson(hero, level);
    }

    /// <summary>
    /// Everything a player has that adds to its stats: the gear it wears, the titles it has chosen for its abilities and, last,
    /// the buffs it carries.
    /// </summary>
    public StatSheet SheetOf(World world, Entity player, Hero hero)
    {
        var sheet = new StatSheet();

        if (_gear is not null && world.TryGet<InventoryComponent>(player, out var inventory))
        {
            _gear.Collect(inventory.Value, sheet);
        }

        if (world.TryGet<TitleComponent>(player, out var title))
        {
            AddTitles(sheet, title, hero);
        }

        // A booster of the status kind raises stats for as long as the person has it.
        if (boosters is not null && world.TryGet<BoosterComponent>(player, out var booster) && booster.Book is { } boosterBook)
        {
            foreach (var entry in boosterBook.Boosters)
            {
                AddBooster(sheet, entry.Booster);
            }
        }

        if (world.TryGet<BuffBookComponent>(player, out var buffs) && buffs.Book is { } book)
        {
            sheet.Apply(book.StatEffects);
        }

        return sheet;
    }

    /// <summary>
    /// The stats a booster gives: each of its status effects names a stat effect (as the effects of items and buffs do) and its value.
    /// </summary>
    private void AddBooster(StatSheet sheet, short booster)
    {
        if (boosters is null || !boosters.TryGet(booster, out var definition))
        {
            return;
        }

        foreach (var effect in definition.Effects)
        {
            if (effect.Effect == BoosterEffect.Status)
            {
                sheet.Apply((int)effect.Apply, effect.Value);
            }
        }
    }

    /// <summary>
    /// What wearing an item would add, for the share of its stats its endurance leaves (1 while it is whole).
    /// </summary>
    public float EnduranceShare(in ItemRow row, ItemInstance item) => _gear?.EnduranceShare(row, item) ?? 1f;

    private void AddTitles(StatSheet sheet, in TitleComponent title, Hero hero)
    {
        if (titles is null)
        {
            return;
        }

        sheet.Apply(Effects(titles, title.AbilityPrimary, hero));
        sheet.Apply(Effects(titles, title.AbilitySecondary, hero));
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

    private static StatEffect[] Effects(TitleCatalog catalog, int title, Hero hero)
    {
        return title > 0 && catalog.TryGet(title, out var definition) && definition.CanBeUsedBy(hero)
            ? definition.Effects
            : [];
    }

    private MonsterCombatComponent Create(in MonsterRow row)
    {
        var stats = Statuses.TryGetValue((short)row.StatusId, out var status)
            ? CombatStatCalculator.Calculate(status, (byte)row.Level, (byte)row.StatusType, StatBonuses.From(row))
            : new CombatStats { Level = (byte)row.Level, MaxHealth = Math.Max(1, row.AddHealth) };

        return new MonsterCombatComponent
        {
            Stats = stats,
            Defense = row.Type == DefensiveWeaponMonster ? DefenseType.Invincible : (DefenseType)row.DefenseType,
            BodyRadius = row.CollisionRadius > 0 ? row.CollisionRadius : DefaultBodyRadius,
            BodyHeight = row.CollisionHeight > 0 ? row.CollisionHeight : DefaultBodyHeight,
            CountsHits = row.Type == HitCountingMonster,
            HoldsSector = HoldsSector(row),
            Rank = row.Rank,
            Faction = row.Faction,
            StatusType = (byte)row.StatusType,
            WeightRank = row.WeightRank,
            ReactionLimit = (ReactionLimit)(row.ReactionLimit & (byte)ReactionLimit.Immovable),
            ArmorBreakMotion = row.SuperArmorBreakMotion == ArmorBreakMotionType,
            StandType = (byte)row.StandType,
            IsDefensiveWeapon = row.Type == DefensiveWeaponMonster
        };
    }

    private static bool HoldsSector(in MonsterRow row)
    {
        return row.Type is PlainMonster or EquippedMonster or BrokenPartsMonster && row.ClearConditionType == 0;
    }
}
