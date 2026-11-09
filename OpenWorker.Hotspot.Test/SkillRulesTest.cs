using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a player may use and learn: the skill must be its own and in its deck, its cooldown over and its cost payable;
/// learning goes one level after the other and costs points, and levels give points.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class SkillRulesTest
{
    private const int Strike = 12001211;
    private const int StrikeTwo = 12001212;
    private const int Unlearned = 12000411;
    private const int Evade = 12040011;

    private static SkillRow Active(int id, int group, byte level, int cost = 35, int cooldown = 5000, byte points = 0) =>
        TestRows.Build<SkillRow>(
            ("Id", id), ("Group", group), ("Level", level), ("UsePosition", 1), ("UseClass", 1), ("RequiredLevel", 1),
            ("RequiredPoints", points), ("SkillType", 2), ("CostAttribute", 2), ("Cost", cost), ("Cooldown", cooldown),
            ("CooldownGroup", group), ("GlobalCooldown", 500));

    private static readonly ReadOnlyCollection<SkillRow> Skills = new(
    [
        Active(Strike, Strike, 1),
        Active(StrikeTwo, Strike, 2, points: 2),
        Active(Unlearned, Unlearned, 1),
        // A move every hero has: owned and used without a deck, paid with stamina.
        TestRows.Build<SkillRow>(("Id", Evade), ("Group", Evade), ("Level", 1), ("UsePosition", 1), ("UseClass", 1), ("SkillType", 1),
            ("PassiveType", 4), ("CostAttribute", 3), ("Cost", 20), ("CooldownGroup", Evade), ("GlobalCooldown", 100))
    ]);

    private static CombatTables EmptyTables() =>
        new(new ReadOnlyCollection<MonsterRow>([]), new ReadOnlyCollection<StatusRow>([]), Skills, new ReadOnlyCollection<CharacterInfoRow>([]));

    private static (World World, Entity Player) Create(float soul = 100, float stamina = 100, short free = 0, short total = 0, byte level = 1)
    {
        var world = World.Create();
        var player = world.Create(
            new AbilityComponent
            {
                Health = new AbilityComponentEntry { Current = 100, Max = 100 },
                SoulGain = new AbilityComponentEntry { Current = (int)soul, Max = 200 },
                SoulVapor = new AbilityComponentEntry { Current = 0, Max = 0 },
                Stamina = new AbilityComponentEntry { Current = (int)stamina, Max = 100 },
                SuperArmor = new AbilityComponentEntry { Current = 0, Max = 0 },
                Speed = new SpeedComponentEntry { Attack = 1, Move = 1 }
            },
            new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
            new LevelComponent(level, 0),
            new GaugeComponent { SoulGauge = soul, Stamina = stamina, Ready = true, SoulCounted = true },
            new SkillCooldownComponent(),
            new SkillPointComponent { TotalSkillPoint = total, FreeSkillPoint = free },
            new SkillLearnedComponent { Values = [new SkillInfoValue { Skill = Strike }, new SkillInfoValue { Skill = Unlearned + 1 }] },
            new SkillDeckComponent { SlotCount = 3, SlotList = [new SkillDeckValue { Position = 0, Skill1 = Strike }] },
            new DirtyComponent(new PersonDirty(1)));

        return (world, player);
    }

    [TestMethod]
    public void ASkillNotLearnedOrNotInTheDeckIsRefusedAndChangesNothing()
    {
        var (world, player) = Create();

        try
        {
            var use = new SkillUse(world, Skills, new PlayerGauges(world, EmptyTables()));

            Assert.AreEqual(SkillRefusal.NotOwned, use.Check(player, Unlearned, TimeSpan.Zero), "not learned");
            Assert.AreEqual(SkillRefusal.NotOwned, use.Check(player, 99999999, TimeSpan.Zero), "not in the table");

            // Learned, but taken out of the deck.
            world.Set(player, world.Get<SkillDeckComponent>(player) with { SlotList = [] });
            Assert.AreEqual(SkillRefusal.NotOwned, use.Check(player, Strike, TimeSpan.Zero), "not in the deck");
            Assert.AreEqual(100f, world.Get<GaugeComponent>(player).SoulGauge);

            // A move of the hero needs no deck.
            Assert.AreEqual(SkillRefusal.None, use.Check(player, Evade, TimeSpan.Zero));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A buff that holds the player (a stun) refuses every skill with the code of a player who cannot attack, and nothing is
    /// taken.
    /// </summary>
    [TestMethod]
    public void AHeldPlayerCannotStartASkill()
    {
        var (world, player) = Create();

        try
        {
            var use = new SkillUse(world, Skills, new PlayerGauges(world, EmptyTables()));
            var book = new BuffBook();
            var stun = new BuffDefinition(1, 10, BuffKind.Debuff, BuffEffect.Stun, 1, 10000, 1, 3f, 0f, 0f, 0, 1, 0, 0, 0, default, default, default);

            Assert.IsTrue(BuffRules.Apply(book, new BuffCatalog([stun]), stun, 0, new BuffCarrier(1, 100, 100, false), []));
            world.Add(player, new BuffBookComponent { Book = book });

            Assert.AreEqual(SkillRefusal.CannotAttack, use.Check(player, Strike, TimeSpan.Zero));
            Assert.AreEqual(SkillRefusal.CannotAttack, use.Check(player, Evade, TimeSpan.Zero));
            Assert.AreEqual(100f, world.Get<GaugeComponent>(player).SoulGauge);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// The bonus a deck page gives shortens the cooldown of the skills in the deck, and a buff lowers what a skill costs in soul
    /// gauge.
    /// </summary>
    [TestMethod]
    public void TheDeckBonusAndBuffsShortenCooldownsAndLowerCosts()
    {
        var (world, player) = Create();

        try
        {
            var bonuses = new ReadOnlyCollection<DeckBonusRow>(
            [
                TestRows.Build<DeckBonusRow>(("Id", 2), ("BonusType", (byte)DeckBonusKind.Cooldown), ("RequiredLevel", 1), ("BonusValue", 0.5f))
            ]);
            var use = new SkillUse(world, Skills, new PlayerGauges(world, EmptyTables()), bonuses);
            var book = new BuffBook();
            var thrift = new BuffDefinition(2, 10, BuffKind.Buff, BuffEffect.SoulCost, 2, 10000, 1, 30f, 0f, 0f, 0, 1, 0, 0, 0, new BuffStat(0, 20), default, default);

            BuffRules.Apply(book, new BuffCatalog([thrift]), thrift, 0, new BuffCarrier(1, 100, 100, false), []);
            world.Add(player, new BuffBookComponent { Book = book });
            world.Add(player, new SkillDeckBonusComponent { Values = [2] });

            use.Use(player, Strike, TimeSpan.Zero);

            Assert.AreEqual(72f, world.Get<GaugeComponent>(player).SoulGauge, "35 less a fifth.");
            Assert.AreEqual(SkillRefusal.Cooldown, use.Check(player, Strike, TimeSpan.FromMilliseconds(2499)));
            Assert.AreEqual(SkillRefusal.None, use.Check(player, Strike, TimeSpan.FromMilliseconds(2500)), "Half of 5 s.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public void ACooldownRefusesTheSkillUntilItIsOver()
    {
        var (world, player) = Create();

        try
        {
            var use = new SkillUse(world, Skills, new PlayerGauges(world, EmptyTables()));

            Assert.AreEqual(SkillRefusal.None, use.Check(player, Strike, TimeSpan.Zero));
            use.Use(player, Strike, TimeSpan.Zero);

            Assert.AreEqual(65f, world.Get<GaugeComponent>(player).SoulGauge, "the cost is taken");
            Assert.AreEqual(SkillRefusal.Cooldown, use.Check(player, Strike, TimeSpan.FromMilliseconds(4999)));
            Assert.AreEqual(SkillRefusal.None, use.Check(player, Strike, TimeSpan.FromMilliseconds(5000)));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public void ASkillWhoseCostCannotBePaidIsRefusedAndTakesNothing()
    {
        var (world, player) = Create(soul: 34, stamina: 19);

        try
        {
            var use = new SkillUse(world, Skills, new PlayerGauges(world, EmptyTables()));

            Assert.AreEqual(SkillRefusal.NoSoulGauge, use.Check(player, Strike, TimeSpan.Zero));
            Assert.AreEqual(SkillRefusal.NoStamina, use.Check(player, Evade, TimeSpan.Zero));
            Assert.AreEqual(34f, world.Get<GaugeComponent>(player).SoulGauge);
            Assert.AreEqual(19f, world.Get<GaugeComponent>(player).Stamina);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public void TheSoulGaugeFillsByHitsAndIsSpentByItsSkill()
    {
        var (world, player) = Create(soul: 0);

        try
        {
            var gauges = new PlayerGauges(world, EmptyTables());
            var use = new SkillUse(world, Skills, gauges);

            // A hit of a skill that absorbs 3.27 on a monster that lets go of three times as much, up to the size of the gauge.
            var hit = SoulGauge.Absorbed(3.27f, 3f, 0f, 200f);

            Assert.AreEqual(9.81f, hit, 0.001f);
            Assert.AreEqual(0f, SoulGauge.Absorbed(3.27f, 3f, 200f, 200f), "a full gauge takes nothing");

            Assert.AreEqual(SkillRefusal.NoSoulGauge, use.Check(player, Strike, TimeSpan.Zero));

            for (var i = 0; i < 4; i++)
            {
                gauges.Absorb(player, hit, 200f);
            }

            Assert.AreEqual(SkillRefusal.None, use.Check(player, Strike, TimeSpan.Zero));
            use.Use(player, Strike, TimeSpan.Zero);

            Assert.AreEqual(4 * hit - 35f, world.Get<GaugeComponent>(player).SoulGauge, 0.001f);
            Assert.AreEqual((int)(4 * hit - 35f), world.Get<AbilityComponent>(player).SoulGain.Current, "the character info follows");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public void LearningGoesOneLevelAfterTheOtherAndCostsItsPoints()
    {
        var (world, player) = Create(free: 1, total: 5);

        try
        {
            var learning = new SkillLearning(world, Skills, new ReadOnlyCollection<DivergenceRow>([]), new ReadOnlyCollection<LevelSkillPointRow>([]));

            // The second level asks for two points: refused, and the book stays as it was.
            Assert.AreEqual(SkillRefusal.CannotLearn, learning.Learn(player, StrikeTwo).Refusal);
            Assert.AreEqual(Strike, world.Get<SkillLearnedComponent>(player).Values[0].Skill);
            Assert.AreEqual((short)1, world.Get<SkillPointComponent>(player).FreeSkillPoint);

            world.Set(player, new SkillPointComponent { TotalSkillPoint = 5, FreeSkillPoint = 3 });

            var (refusal, replaced) = learning.Learn(player, StrikeTwo);

            Assert.AreEqual(SkillRefusal.None, refusal);
            Assert.AreEqual(Strike, replaced);
            Assert.AreEqual(StrikeTwo, world.Get<SkillLearnedComponent>(player).Values[0].Skill);
            Assert.AreEqual((short)1, world.Get<SkillPointComponent>(player).FreeSkillPoint);
            Assert.AreEqual(StrikeTwo, world.Get<SkillDeckComponent>(player).SlotList[0].Skill1, "the deck holds the new level");

            // The same level again is no next level.
            Assert.AreEqual(SkillRefusal.CannotLearn, learning.Learn(player, StrikeTwo).Refusal);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public void ALevelGivesItsSkillPoints()
    {
        var (world, player) = Create(free: 0, total: 0, level: 1);

        try
        {
            // Levels 2, 3 and 4 give 0, 1 and 3 points.
            var levels = new ReadOnlyCollection<LevelSkillPointRow>([Level(1, 0), Level(2, 0), Level(3, 1), Level(4, 3)]);
            var learning = new SkillLearning(world, Skills, new ReadOnlyCollection<DivergenceRow>([]), levels);

            learning.OnLevelReached(player, 1, 4);

            Assert.AreEqual(new SkillPointComponent { TotalSkillPoint = 4, FreeSkillPoint = 4 }, world.Get<SkillPointComponent>(player));

            // A person loaded at level 4 with no points gets what its level has given, once.
            world.Set(player, new SkillPointComponent { TotalSkillPoint = 0, FreeSkillPoint = 0 });
            world.Set(player, new LevelComponent(4, 0));

            learning.CatchUp(player);
            learning.CatchUp(player);

            Assert.AreEqual(new SkillPointComponent { TotalSkillPoint = 4, FreeSkillPoint = 4 }, world.Get<SkillPointComponent>(player));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private static LevelSkillPointRow Level(byte level, byte points)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(level);
        writer.Write(level);
        writer.Write(0);
        writer.Write(points);
        writer.Write((ushort)50);
        writer.Flush();
        stream.Position = 0;

        return new LevelSkillPointRow(new BinaryReader(stream));
    }
}
