using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Lua;
using OpenWorker.Server.Common.Services;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The hidden events of a maze: how a box starts one and picks its condition, how each kind of condition counts, and how the
/// clear decides an event still going, and where the server counts each kind.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class HiddenEventTest
{
    private const short Box = 6001;
    private const int Monster = 10101001;
    private const int PaidItem = 820000002;
    private const int Strike = 12001211;
    private const int JumpStrike = 12001311;
    private const int Evade = 12040011;

    private static T Row<T>(Action<BinaryWriter> write) where T : struct
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return (T)Activator.CreateInstance(typeof(T), reader)!;
    }

    private static HiddenEventRow Event(short id, params (int Condition, short Rate)[] conditions) => Row<HiddenEventRow>(w =>
    {
        w.Write(id);

        for (var i = 0; i < 5; i++)
        {
            w.Write(i < conditions.Length ? conditions[i].Condition : 0);
        }

        for (var i = 0; i < 5; i++)
        {
            w.Write(i < conditions.Length ? conditions[i].Rate : (short)0);
        }

        w.Write(819999999);
        w.Write((byte)1);
    });

    private static EventConditionRow Condition(int id, HiddenEventKind kind, HiddenEventTarget target, int targetId, int count) => Row<EventConditionRow>(w =>
    {
        w.Write(id);
        w.Write((short)6000);
        w.Write((byte)kind);
        w.Write((byte)target);
        w.Write(targetId);
        w.Write(count);
    });

    private static HiddenEventRewardRow Reward(ushort id, int item, byte count) => Row<HiddenEventRewardRow>(w =>
    {
        w.Write(id);
        w.Write(new byte[5 * 6]);
        w.Write(item);
        w.Write(count);
    });

    private static HiddenEventCatalog Catalog(HiddenEventRow[] events, params EventConditionRow[] conditions) => new(
        new ReadOnlyCollection<HiddenEventRow>(events),
        new ReadOnlyCollection<EventConditionRow>(conditions),
        new ReadOnlyCollection<HiddenEventRewardRow>([Reward((ushort)Box, PaidItem, 2)]),
        Skills,
        new ReadOnlyCollection<DeckBonusRow>([TestRows.Build<DeckBonusRow>(("Id", (short)2), ("RequiredLevel", (byte)1))]),
        random: new Random(7));

    private static readonly ReadOnlyCollection<SkillRow> Skills = new(
    [
        TestRows.Build<SkillRow>(("Id", Strike), ("Group", Strike), ("Level", (byte)1), ("UsePosition", (byte)1), ("SkillType", (byte)2),
            ("CostAttribute", (byte)2), ("Cost", 35), ("CooldownGroup", Strike)),
        TestRows.Build<SkillRow>(("Id", JumpStrike), ("Group", JumpStrike), ("Level", (byte)1), ("UsePosition", (byte)1), ("SkillType", (byte)2),
            ("PassiveType", (byte)16), ("CooldownGroup", JumpStrike)),
        TestRows.Build<SkillRow>(("Id", Evade), ("Group", Evade), ("Level", (byte)1), ("UsePosition", (byte)1), ("SkillType", (byte)1),
            ("PassiveType", (byte)4), ("CostAttribute", (byte)3), ("Cost", 20), ("CooldownGroup", Evade))
    ]);

    /// <summary>
    /// What the server counted, wherever it heard of it.
    /// </summary>
    private static HiddenEventCounter Recorder(List<(HiddenEventTarget Target, int Amount)> counted) => new()
    {
        Route = (_, target, _, amount) => counted.Add((target, amount))
    };

    /// <summary>
    /// A maze with a monster whose hidden event asks for so many of the target and has started.
    /// </summary>
    private static LuaMaze Started(World world, Entity player, HiddenEventTarget target, int count)
    {
        var catalog = Catalog([Event(Box, (1, 1))], Condition(1, HiddenEventKind.Reach, target, 0, count));
        var maze = TestMazes.Create(world, player, map: PartyMazeTest.MapWithMonster(Monster), hiddenEvents: catalog);

        maze.OnHiddenEventBox(player, Box, 10000f);
        maze.SpawnSector(1);

        return maze;
    }

    private static HiddenEvent Running(HiddenEventKind kind, HiddenEventTarget target, int targetId, int count)
    {
        var started = Catalog([Event(Box, (1, 1))], Condition(1, kind, target, targetId, count)).Pick(Box)!;

        started.Begin();

        return started;
    }

    [TestMethod]
    public void ABoxStartsItsEventByItsChanceWithAConditionTheServerCounts()
    {
        // A target the server does not know is never picked: the kill is picked whatever its weight.
        var catalog = Catalog(
            [Event(Box, (1, 9000), (2, 1000)), Event((short)(Box + 1), (1, 10000))],
            Condition(1, HiddenEventKind.Reach, (HiddenEventTarget)99, 0, 10),
            Condition(2, HiddenEventKind.Kill, HiddenEventTarget.Monster, Monster, 1));

        Assert.IsNull(catalog.TryStart(default(Entity), Box, 0f), "a box whose chance is nothing starts nothing");
        Assert.IsNull(catalog.Pick(Box + 1), "an event with no condition the server counts never starts");

        for (var i = 0; i < 20; i++)
        {
            var started = catalog.TryStart(default(Entity), Box, 10000f);

            Assert.IsNotNull(started, "a certain chance always starts the event");
            Assert.AreEqual(2, started.Condition.Id);
            Assert.AreEqual(HiddenEventState.Started, started.State);

            started.Begin();
            Assert.IsTrue(started.Count(HiddenEventTarget.Monster, Monster, 1));
            Assert.AreEqual(new HiddenEventReward(PaidItem, 2), started.Reward, "the reward is the server's");
        }
    }

    [TestMethod]
    public void EachKindOfConditionCountsWhatItNames()
    {
        var kill = Running(HiddenEventKind.Kill, HiddenEventTarget.Monster, Monster, 2);

        Assert.IsFalse(kill.Count(HiddenEventTarget.Monster, Monster + 1, 1), "another monster");
        Assert.IsFalse(kill.Count(HiddenEventTarget.Gold, Monster, 1), "another target");
        Assert.IsTrue(kill.Count(HiddenEventTarget.Monster, Monster, 1));
        Assert.AreEqual(HiddenEventState.Progress, kill.State);
        Assert.IsTrue(kill.Count(HiddenEventTarget.Monster, Monster, 1));
        Assert.AreEqual(HiddenEventState.Met, kill.State);
        Assert.IsFalse(kill.Count(HiddenEventTarget.Monster, Monster, 1), "a met event counts no more");

        var combo = Running(HiddenEventKind.Reach, HiddenEventTarget.MaxCombo, 0, 50);

        Assert.IsTrue(combo.Count(HiddenEventTarget.MaxCombo, 0, 30));
        Assert.IsFalse(combo.Count(HiddenEventTarget.MaxCombo, 0, 12), "a shorter combo keeps the longest");
        Assert.AreEqual(30, combo.Counted);
        Assert.IsTrue(combo.Count(HiddenEventTarget.MaxCombo, 0, 50));
        Assert.AreEqual(HiddenEventState.Met, combo.State);

        var gold = Running(HiddenEventKind.Collect, HiddenEventTarget.Gold, 0, 100);

        Assert.IsTrue(gold.Count(HiddenEventTarget.Gold, 800000001, 60));
        Assert.IsTrue(gold.Count(HiddenEventTarget.Gold, 800000001, 60));
        Assert.AreEqual(HiddenEventState.Met, gold.State, "amounts add up");
    }

    [TestMethod]
    public void TheClearMeetsAnEventThatOnlyAskedToKeepUnderACountAndFailsTheRest()
    {
        var untouched = Running(HiddenEventKind.UseLess, HiddenEventTarget.ErosionSkill, 0, 0);

        Assert.IsTrue(untouched.Finish());
        Assert.AreEqual(HiddenEventState.Met, untouched.State);

        var used = Running(HiddenEventKind.UseLess, HiddenEventTarget.ErosionSkill, 0, 0);

        Assert.IsTrue(used.Count(HiddenEventTarget.ErosionSkill, 0, 1));
        Assert.AreEqual(HiddenEventState.Failed, used.State, "one use past the count fails at once");
        Assert.IsFalse(used.Finish());
        Assert.IsNull(used.Reward);

        var unfinished = Running(HiddenEventKind.Kill, HiddenEventTarget.Monster, 0, 5);

        unfinished.Count(HiddenEventTarget.Monster, Monster, 1);

        Assert.IsTrue(unfinished.Finish());
        Assert.AreEqual(HiddenEventState.Failed, unfinished.State, "a count not reached by the clear fails");
        Assert.IsNull(unfinished.Reward);
    }

    /// <summary>
    /// A jump counts once the others have been told of it; a jump the server drops does not.
    /// </summary>
    [TestMethod]
    public async Task AJumpCountsWhenTheOthersAreToldOfIt()
    {
        using var district = new DistrictHarness();
        var counted = new List<(HiddenEventTarget Target, int Amount)>();
        var movement = new MovementService(district.Ecs, new ChannelAudience(district.Ecs, district.Channels), district.Pace, district.Errors,
            NullLogger<MovementService>.Instance, Recorder(counted));
        var player = district.Join(0f, 0f);
        var context = new ServiceHandleContext(player.Entity, CancellationToken.None);

        await movement.OnHandleAsync(context, Jump(player.Actor, 0x0001_2714_0000_0000));

        Assert.IsEmpty(counted, "a jump on a map the player left is dropped");

        await movement.OnHandleAsync(context, Jump(player.Actor, 0x0001_2713_0000_0000));

        CollectionAssert.AreEqual(new[] { (HiddenEventTarget.Jump, 1) }, counted);
    }

    private static MovementJumpRequest Jump(int actor, long map)
    {
        Span<byte> bytes = stackalloc byte[4 + 8 + (6 * 4) + 2];

        var writer = new PacketWriter(bytes);

        writer.WriteInt32(actor);
        writer.WriteInt64(map);
        writer.WriteSingle(10f);
        writer.WriteSingle(0f);
        writer.WriteSingle(0f);
        writer.WriteSingle(0f);
        writer.WriteSingle(20f);
        writer.WriteSingle(0f);
        writer.WriteByte(0);
        writer.WriteByte(0);

        var reader = new PacketReader(bytes);

        return MovementJumpRequest.Read(ref reader);
    }

    /// <summary>
    /// The stamina a skill or a move costs counts, as much as it costs; a skill paid with the soul gauge counts none.
    /// </summary>
    [TestMethod]
    public void TheStaminaASkillCostsCounts()
    {
        var world = World.Create();

        try
        {
            var life = new AbilityComponentEntry { Current = 100, Max = 100 };
            var player = world.Create(
                new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Attack = 1, Move = 1 } },
                new PersonInfoComponent { Name = "Haru", Hero = Hero.Haru },
                new LevelComponent(1, 0),
                new GaugeComponent { SoulGauge = 100, Stamina = 100, Ready = true, SoulCounted = true },
                new SkillCooldownComponent());
            var tables = new CombatTables(TestMazes.None<MonsterRow>(), TestMazes.None<StatusRow>(), Skills, TestMazes.None<CharacterInfoRow>());
            var counted = new List<(HiddenEventTarget Target, int Amount)>();
            var use = new SkillUse(world, Skills, new PlayerGauges(world, tables), hiddenEvents: Recorder(counted));

            use.Use(player, Strike, TimeSpan.Zero);
            use.Use(player, Evade, TimeSpan.FromSeconds(10));

            CollectionAssert.AreEqual(new[] { (HiddenEventTarget.Stamina, 20) }, counted);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A blow of a player that breaks the armor of a monster counts; a blow on an armor already broken, one the armor holds, and
    /// the blow of a helper do not.
    /// </summary>
    [TestMethod]
    public void ABlowOfAPlayerThatBreaksAnArmorCounts()
    {
        var world = World.Create();

        try
        {
            var player = MonsterArmorTest.Player(world);
            var maze = Started(world, player, HiddenEventTarget.ArmorBreak, 5);
            var monster = maze.Creatures.FindMonsters((_, _) => true).Single();

            maze.CountHiddenEventBlow(player, monster, Strike, armorWasBroken: false, killed: false, aloft: false);

            Assert.AreEqual(0, maze.HiddenEvent!.Counted, "the armor held");

            world.Set(monster, world.Get<MonsterCombatComponent>(monster) with { SuperArmorBrokenUntil = TimeSpan.FromSeconds(5) });

            maze.CountHiddenEventBlow(player, monster, Strike, armorWasBroken: false, killed: false, aloft: false);
            maze.CountHiddenEventBlow(player, monster, Strike, armorWasBroken: true, killed: false, aloft: false);
            maze.CountHiddenEventBlow(monster, monster, Strike, armorWasBroken: false, killed: false, aloft: false);

            Assert.AreEqual(1, maze.HiddenEvent.Counted);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A skill used from the second, third or fourth place of the row of the deck the client names, with a bonus of the table
    /// chosen for the place, is linked; the first place, another row and a place without a bonus are not.
    /// </summary>
    [TestMethod]
    public void ASkillUsedAfterTheFirstPlaceOfItsDeckRowWithABonusIsLinked()
    {
        var catalog = Catalog([Event(Box, (1, 1))]);
        SkillDeckValue[] decks = [new SkillDeckValue { Position = 1, Skill1 = Evade, Skill2 = Strike, Skill3 = JumpStrike }];
        ushort[] bonuses = [2, 0, 0, 0];

        Assert.IsTrue(catalog.IsDeckLink(decks, bonuses, Strike, 1));
        Assert.IsFalse(catalog.IsDeckLink(decks, bonuses, Evade, 1), "the first place links to nothing");
        Assert.IsFalse(catalog.IsDeckLink(decks, bonuses, Strike, 0), "another row");
        Assert.IsFalse(catalog.IsDeckLink(decks, bonuses, JumpStrike, 1), "no bonus chosen for the place");
        Assert.IsFalse(catalog.IsDeckLink(decks, [9, 9, 9, 9], Strike, 1), "a bonus the table does not have");
        Assert.IsFalse(catalog.IsDeckLink(decks, bonuses, Strike, 6), "past the rows of a deck");
    }

    /// <summary>
    /// A blow of a skill that kills a monster in the air counts a kill in the air, and one of a jump attack a kill by a jump
    /// attack; a blow that does not kill counts neither.
    /// </summary>
    [TestMethod]
    public void AKillCountsByWhereTheMonsterWasAndTheSkillThatKilledIt()
    {
        var world = World.Create();

        try
        {
            var player = MonsterArmorTest.Player(world);
            var inAir = Started(world, player, HiddenEventTarget.KillInAir, 5);
            var monster = inAir.Creatures.FindMonsters((_, _) => true).Single();

            inAir.CountHiddenEventBlow(player, monster, Strike, armorWasBroken: false, killed: false, aloft: true);
            inAir.CountHiddenEventBlow(player, monster, Strike, armorWasBroken: false, killed: true, aloft: false);

            Assert.AreEqual(0, inAir.HiddenEvent!.Counted, "a hit in the air, a kill on the ground");

            inAir.CountHiddenEventBlow(player, monster, Strike, armorWasBroken: false, killed: true, aloft: true);

            Assert.AreEqual(1, inAir.HiddenEvent.Counted);

            var jumping = Started(world, player, HiddenEventTarget.JumpAttackKill, 5);

            jumping.CountHiddenEventBlow(player, monster, JumpStrike, armorWasBroken: false, killed: false, aloft: false);
            jumping.CountHiddenEventBlow(player, monster, Strike, armorWasBroken: false, killed: true, aloft: false);
            jumping.CountHiddenEventBlow(player, monster, JumpStrike, armorWasBroken: false, killed: true, aloft: false);

            Assert.AreEqual(1, jumping.HiddenEvent!.Counted);
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
