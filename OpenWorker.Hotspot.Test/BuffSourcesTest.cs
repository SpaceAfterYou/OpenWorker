using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Boosters.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Lua;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The buffs a player has without being hit by anything (passive skills and cards, combos, auras, boosters), what holds a buff
/// back (a resistance, the place), what a buff does to the damage of a hit, and the speeds and resistances in the stats. The
/// tables are made by the test.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BuffSourcesTest
{
    private const ushort Stun = 800;
    private const ushort Anywhere = 801;
    private const ushort MazeOnly = 802;
    private const ushort Quickness = 810;
    private const ushort PassiveMight = 820;
    private const ushort CardMight = 821;
    private const ushort Haste = 830;
    private const ushort ComboMight = 840;
    private const ushort Blood = 850;
    private const ushort Mirror = 851;
    private const ushort Slayer = 852;
    private const ushort ImmuneStun = 860;

    private const byte AttackShare = 34;
    private const byte MoveSpeedShare = 31;
    private const byte StunResistance = 70;

    private const int PassiveSkill = 31000;
    private const int Card = 38100;
    private const int Aura = 500;
    private const int ComboAt = 3;

    private static BuffRow Row(ushort id, BuffKind kind, BuffEffect effect, int duration, byte places = 10, byte stat = 0, int value = 0, byte stat2 = 0, int value2 = 0) =>
        TestRows.Build<BuffRow>(
            ("Id", id), ("AllowedPlaces", places), ("Kind", (byte)kind), ("Effect", (ushort)effect), ("Group", id), ("SuccessRate", 10000), ("Grade", (byte)1),
            ("DurationMilliseconds", duration), ("OverlapCount", (byte)1), ("Stat1", stat), ("Value1", value), ("Stat2", stat2), ("Value2", value2));

    private static readonly ReadOnlyCollection<BuffRow> Buffs = new(
    [
        Row(Stun, BuffKind.Debuff, BuffEffect.Stun, 3000),
        Row(Anywhere, BuffKind.Buff, BuffEffect.Stats, 0, places: 1, stat: AttackShare, value: 5),
        Row(MazeOnly, BuffKind.Buff, BuffEffect.Stats, 0, places: 2, stat: AttackShare, value: 5),
        Row(Quickness, BuffKind.Buff, BuffEffect.Stats, 0, stat: MoveSpeedShare, value: 30, stat2: StunResistance, value2: 25),
        Row(PassiveMight, BuffKind.Combined, BuffEffect.Stats, 0, stat: AttackShare, value: 10),
        Row(CardMight, BuffKind.Buff, BuffEffect.Stats, 10000, stat: AttackShare, value: 10),
        Row(Haste, BuffKind.Buff, BuffEffect.Stats, 0, stat: MoveSpeedShare, value: 20),
        Row(ComboMight, BuffKind.Buff, BuffEffect.Stats, 10000, stat: AttackShare, value: 10),
        Row(Blood, BuffKind.Buff, BuffEffect.Blood, 10000, value: 50),
        Row(Mirror, BuffKind.Debuff, BuffEffect.Reflection, 10000, value: 25),
        Row(Slayer, BuffKind.Buff, BuffEffect.BossAttackAdd, 10000, value: 50)
    ]);

    private static Entity Player(World world, int actor, Vector3 position, int health = 1000, params object[] extra)
    {
        var entity = world.Create(
            new ActorComponent(actor, ActorType.User),
            new ServerSessionComponent(new RecordingConnection()),
            new WorldComponent { Location = 0, Position = position, Rotation = 0f, Jump = 0 },
            new LevelComponent(10, 0),
            new PersonInfoComponent { Name = "Haru" + actor, Hero = Hero.Haru },
            new AbilityComponent
            {
                Health = new AbilityComponentEntry { Current = health, Max = 1000 },
                SoulGain = default,
                SoulVapor = default,
                Stamina = default,
                SuperArmor = default,
                Speed = new SpeedComponentEntry { Move = 100f, Attack = 100f }
            });

        foreach (var component in extra)
        {
            switch (component)
            {
                case SkillLearnedComponent learned:
                    world.Add(entity, learned);
                    break;
                case InventoryComponent inventory:
                    world.Add(entity, inventory);
                    break;
                case BuffMemoryComponent memory:
                    world.Add(entity, memory);
                    break;
            }
        }

        return entity;
    }

    private static LuaMaze Maze(World world, Entity player, BuffManager manager) =>
        TestMazes.Create(world, player, map: PartyMazeTest.MapWithMonster(1000), buffs: manager);

    private static BuffManager Manager(
        ReadOnlyCollection<SkillRow>? skills = null,
        ReadOnlyCollection<AkashicRecordsRow>? cards = null,
        ReadOnlyCollection<AuraRow>? auras = null,
        ReadOnlyCollection<ComboBuffRow>? combos = null) =>
        new(Buffs, skills, null, cards, auras, combos);

    private static ReadOnlyCollection<T> Rows<T>(params T[] rows) => new(rows);

    private static bool Has(World world, Entity entity, ushort buff) => BuffBook.Of(world, entity) is { } book && book.IndexOf(buff) >= 0;

    private static CombatStats Stats(World world, Entity player) =>
        new CombatTables(TestMazes.None<MonsterRow>(), TestMazes.None<StatusRow>(), TestMazes.None<SkillRow>(), TestMazes.None<CharacterInfoRow>()).GetPlayer(world, player);

    /// <summary>
    /// A resistance holds back the chance of the state it names by its value, out of 10000; a buff of a system type that
    /// resistances do not hold back sticks all the same.
    /// </summary>
    [TestMethod]
    public void AResistanceHoldsBackTheChanceOfItsState()
    {
        var catalog = new BuffCatalog(Buffs);
        var random = new Random(3);
        var changes = new List<BuffChange>();

        Assert.IsTrue(catalog.TryGet(Stun, out var stun));

        var resistant = new BuffCarrier(1, 100, 100, false, Resist: new Resistances { Stun = 10000 });
        var open = new BuffCarrier(1, 100, 100, false);

        for (var attempt = 0; attempt < 50; attempt++)
        {
            Assert.IsFalse(BuffRules.Give(new BuffBook(), catalog, Stun, 7, resistant, random, changes), "All the chance is resisted.");
            Assert.IsTrue(BuffRules.Give(new BuffBook(), catalog, Stun, 7, open, random, changes));
        }

        // A resistance to another state does nothing to it.
        Assert.IsTrue(BuffRules.Give(new BuffBook(), catalog, Stun, 7, new BuffCarrier(1, 100, 100, false, Resist: new Resistances { Sleep = 10000 }), random, changes));

        // A buff of system type 2 is not rolled at all.
        var system = new BuffCatalog([stun with { Id = ImmuneStun, Group = ImmuneStun, SystemType = 2 }]);

        Assert.IsTrue(BuffRules.Give(new BuffBook(), system, ImmuneStun, 7, resistant, random, changes));
    }

    /// <summary>
    /// A buff stays only where its row allows it: a maze-only buff is refused in a district and taken away when its carrier
    /// comes to one, a buff for anywhere stays.
    /// </summary>
    [TestMethod]
    public void ABuffStaysOnlyWhereItsRowAllowsIt()
    {
        var catalog = new BuffCatalog(Buffs);
        var book = new BuffBook();
        var changes = new List<BuffChange>();
        var inMaze = new BuffCarrier(1, 100, 100, false);
        var inDistrict = new BuffCarrier(1, 100, 100, false, BuffDefinition.District);

        Assert.IsFalse(BuffRules.Give(book, catalog, MazeOnly, 1, inDistrict, new Random(1), changes, roll: false));
        Assert.IsTrue(BuffRules.Give(book, catalog, MazeOnly, 1, inMaze, new Random(1), changes, roll: false));
        Assert.IsTrue(BuffRules.Give(book, catalog, Anywhere, 1, inMaze, new Random(1), changes, roll: false));
        Assert.HasCount(2, book.Buffs);

        BuffRules.ClearForPlace(book, catalog, inDistrict, changes);

        Assert.AreEqual(Anywhere, book.Buffs.Single().Buff);
    }

    /// <summary>
    /// A passive skill of the status kind gives its buff to the player in the maze, the buff goes when the skill does, and a
    /// death that took it does not keep it: the next look puts it back.
    /// </summary>
    [TestMethod]
    public void APassiveSkillGivesItsBuffForAsLongAsThePlayerHasTheSkill()
    {
        var world = World.Create();

        try
        {
            var skills = Rows(TestRows.Build<SkillRow>(("Id", PassiveSkill), ("SkillType", (byte)1), ("PassiveType", (byte)13), ("PassiveValue", (short)PassiveMight)));
            var learned = new SkillLearnedComponent { Values = [new SkillInfoValue { Skill = PassiveSkill }] };
            var player = Player(world, 100, Vector3.Zero, extra: learned);
            var maze = Maze(world, player, Manager(skills));

            Assert.IsTrue(Has(world, player, PassiveMight), "It is on the player from the moment it is in the maze.");

            // A death takes the buffs that are not kept on death; the skill gives it again.
            maze.Buffs.OnDeath(player);
            Assert.IsFalse(Has(world, player, PassiveMight));
            maze.Buffs.Step(1.1f);
            Assert.IsTrue(Has(world, player, PassiveMight));

            // The player no longer has the skill (it was reset).
            world.Set(player, new SkillLearnedComponent { Values = [] });
            maze.Buffs.Step(0.05f);
            Assert.IsFalse(Has(world, player, PassiveMight));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// The buffs a player carries go with it to the next server with the time they had left, but for the ones its skills give
    /// again wherever it is.
    /// </summary>
    [TestMethod]
    public void ABuffGoesWithThePlayerToTheNextServerWithTheTimeItHadLeft()
    {
        var world = World.Create();

        try
        {
            var skills = Rows(TestRows.Build<SkillRow>(("Id", PassiveSkill), ("SkillType", (byte)1), ("PassiveType", (byte)13), ("PassiveValue", (short)PassiveMight)));
            var learned = new SkillLearnedComponent { Values = [new SkillInfoValue { Skill = PassiveSkill }] };
            var player = Player(world, 100, Vector3.Zero, extra: [learned, new BuffMemoryComponent { Book = new BuffMemoryBook() }]);
            var first = Maze(world, player, Manager(skills));

            Assert.IsTrue(first.Buffs.Give(player, CardMight, player));
            first.Buffs.Step(4f);
            first.Buffs.Detach(player);

            var saved = BuffMemory.Saved(world, player).Single();

            Assert.AreEqual(CardMight, saved.Buff, "The buff of the skill is not carried: the skill gives it again.");
            Assert.AreEqual(6f, saved.Time, 0.1f);

            var second = Maze(world, player, Manager(skills));
            var book = BuffBook.Of(world, player)!;

            Assert.IsTrue(Has(world, player, CardMight));
            Assert.AreEqual(6f, book.Buffs.Single(e => e.Buff == CardMight).Remaining, 0.1f);
            Assert.IsTrue(Has(world, player, PassiveMight));

            second.Buffs.Step(7f);
            Assert.IsFalse(Has(world, player, CardMight), "It runs out when its time is over.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A passive card in a quick slot gives its buff when its condition happens, by its chance, and then cools down; a card that
    /// names no condition, or one the player has not put into its book, does nothing.
    /// </summary>
    [TestMethod]
    public void APassiveCardGivesItsBuffByItsConditionAndThenCoolsDown()
    {
        var world = World.Create();

        try
        {
            var cards = Rows(
                TestRows.Build<AkashicRecordsRow>(("Id", Card), ("Position", (short)1), ("InvokeCondition", (byte)1), ("InvokeRate", (short)10000), ("Buff", (int)CardMight), ("Cooldown", 5000)),
                TestRows.Build<AkashicRecordsRow>(("Id", Card + 1), ("Position", (short)2), ("Buff", (int)Haste)),
                TestRows.Build<AkashicRecordsRow>(("Id", Card + 2), ("Position", (short)3), ("InvokeCondition", (byte)1), ("InvokeRate", (short)10000), ("Buff", (int)Haste)));

            // The third card is in a quick slot but not in the book.
            var bag = new Inventory(7) { QuickSlotCards = [Card, Card + 1, Card + 2, 0, 0] };

            bag.AkashicRecords.Records.Add(new AkashicRecord { Card = Card, Position = 1 });
            bag.AkashicRecords.Records.Add(new AkashicRecord { Card = Card + 1, Position = 2 });

            var player = Player(world, 100, Vector3.Zero, extra: new InventoryComponent(bag));
            var maze = Maze(world, player, Manager(cards: cards));

            Assert.IsFalse(Has(world, player, CardMight), "Nothing has happened yet.");

            maze.Buffs.OnCondition(player, 1);
            Assert.IsTrue(Has(world, player, CardMight));
            Assert.IsFalse(Has(world, player, Haste), "A card with no condition is not a passive one, and a card outside the book counts for nothing.");

            maze.Buffs.OnCondition(player, 6);
            maze.Buffs.Step(1f);

            BuffRules.Clear(BuffBook.Of(world, player)!, new BuffCatalog(Buffs), null, new BuffCarrier(100, 1000, 1000, false), []);
            maze.Buffs.OnCondition(player, 1);
            Assert.IsFalse(Has(world, player, CardMight), "The card is cooling down.");

            maze.Buffs.Step(5f);
            maze.Buffs.OnCondition(player, 1);
            Assert.IsTrue(Has(world, player, CardMight), "Cool again.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// An aura gives its buff to the friends of its carrier who are within its reach, now and at every check, and takes nothing
    /// to those farther away; an aura that runs out is gone and so is its message.
    /// </summary>
    [TestMethod]
    public void AnAuraGivesItsBuffToTheFriendsAroundItsCarrier()
    {
        var world = World.Create();

        try
        {
            var auras = Rows(TestRows.Build<AuraRow>(("Id", (short)Aura), ("Target", (byte)2), ("Distance", 500), ("CheckType", (byte)0), ("CheckMilliseconds", 1000),
                ("DurationMilliseconds", 3000), ("Buff", (short)Haste)));
            var carrier = Player(world, 100, Vector3.Zero);
            var near = Player(world, 101, new Vector3(300, 0, 0));
            var far = Player(world, 102, new Vector3(2000, 0, 0));
            var maze = Maze(world, carrier, Manager(auras: auras));

            Assert.IsTrue(maze.Join(near));
            Assert.IsTrue(maze.Join(far));

            Assert.IsTrue(maze.Buffs.SetAura(carrier, Aura));
            Assert.AreEqual(Aura, maze.Buffs.AuraOf(carrier));
            Assert.IsFalse(maze.Buffs.SetAura(carrier, Aura), "The same aura again changes nothing.");

            Assert.IsTrue(Has(world, carrier, Haste), "The friends include the carrier.");
            Assert.IsTrue(Has(world, near, Haste));
            Assert.IsFalse(Has(world, far, Haste), "Out of reach.");

            // The one who walks into its reach later gets the buff at the next check.
            world.Set(far, world.Get<WorldComponent>(far) with { Position = new Vector3(100, 0, 0) });
            maze.Buffs.Step(1.1f);
            Assert.IsTrue(Has(world, far, Haste));

            maze.Buffs.Step(2f);
            Assert.AreEqual(0, maze.Buffs.AuraOf(carrier), "It ran out.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A combo gives its buff when the hits of the player, none more than three seconds after the one before, reach its number.
    /// </summary>
    [TestMethod]
    public void AComboGivesItsBuffWhenTheHitsReachItsNumber()
    {
        var world = World.Create();

        try
        {
            var combos = Rows(TestRows.Build<ComboBuffRow>(("Id", (short)ComboAt), ("FixedBuff", (short)ComboMight)));
            var player = Player(world, 100, Vector3.Zero);
            var maze = Maze(world, player, Manager(combos: combos));

            maze.Buffs.Step(5f);
            maze.Buffs.OnHits(player, 2);
            maze.Buffs.OnHits(player, 1);
            Assert.IsFalse(Has(world, player, ComboMight), "Two hits are not three.");

            maze.Buffs.OnHits(player, 1);
            Assert.IsTrue(Has(world, player, ComboMight));
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// The buffs of a hit's two sides: a buff of blood gives the attacker a share of the damage back as life, a buff of
    /// reflection takes the share of the damage from the target's life once more, and a buff against bosses makes a hit on a
    /// boss harder.
    /// </summary>
    [TestMethod]
    public void TheBuffsOfBloodReflectionAndBossesChangeWhatAHitDoes()
    {
        var world = World.Create();

        try
        {
            var attacker = Player(world, 100, Vector3.Zero, health: 500);
            var maze = Maze(world, attacker, Manager());

            maze.SpawnSector(1);

            var monster = maze.Creatures.FindMonsters((_, _) => true).Single();

            world.Set(monster, world.Get<CreatureComponent>(monster) with { Health = 1000 });

            var before = world.Get<CreatureComponent>(monster).Health;

            Assert.IsTrue(maze.Buffs.Give(attacker, Blood, attacker));
            Assert.IsTrue(maze.Buffs.Give(monster, Mirror, attacker));

            maze.Buffs.AfterHit(attacker, monster, 0, 100, default);

            Assert.AreEqual(550, world.Get<AbilityComponent>(attacker).Health.Current, "Half of the 100 damage comes back.");
            Assert.AreEqual(before - 25, world.Get<CreatureComponent>(monster).Health, "A quarter of the damage is taken from the target again.");

            Assert.AreEqual(100, maze.Buffs.Adjust(attacker, monster, 100), "The monster is no boss.");

            world.Set(monster, world.Get<MonsterCombatComponent>(monster) with { Rank = 3 });
            Assert.IsTrue(maze.Buffs.Give(attacker, Slayer, attacker));
            Assert.AreEqual(150, maze.Buffs.Adjust(attacker, monster, 100), "Half as much again against a boss.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

}
