using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Helpers.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Helpers;
using OpenWorker.Lua.Managers;
using OpenWorker.MazeServer.Services;
using OpenWorker.Navigation;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A helper called into a maze: the rules of calling it, what every player of the maze sees of it, that it goes with its
/// owner, that what it kills is its owner's, and that it fights with what it wears now.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class HelperSummonTest
{
    /// <summary>
    /// Mirium, the first helper a person gets.
    /// </summary>
    private const int Mirium = 10105166;

    private static readonly TimeSpan Long = TimeSpan.FromMinutes(1);

    /// <summary>
    /// A call goes through the checks of the game in their order: a person who is down gets nothing at all, then the delay
    /// between calls, being in a maze, a maze that takes helpers and a helper of its own not called yet, and the room: four
    /// bodies at most, the people of the party and every helper in the maze. Alone that is three helpers, in a party of two
    /// two, and a party of four has no room at all.
    /// </summary>
    [TestMethod]
    public void ACallIsCheckedInTheOrderOfTheGameAndAMazeHoldsFourBodies()
    {
        var call = new HelperSummonCase(true, Long, true, true, true, false, 0, 1);

        Assert.IsNull(HelperRules.CheckSummon(call with { OwnerAlive = false, SinceLastSummon = TimeSpan.Zero, InMaze = false }));
        Assert.AreEqual(HelperRules.Cooldown, HelperRules.CheckSummon(call with { SinceLastSummon = TimeSpan.FromSeconds(9), InMaze = false, Owned = false }));
        Assert.AreEqual(HelperManager.NotInMaze, HelperRules.CheckSummon(call with { InMaze = false, Owned = false, HelpersInMaze = 3 }));
        Assert.AreEqual(HelperManager.CannotSummon, HelperRules.CheckSummon(call with { MazeTakesHelpers = false, HelpersInMaze = 3 }));
        Assert.AreEqual(HelperManager.CannotSummon, HelperRules.CheckSummon(call with { Owned = false, HelpersInMaze = 3 }));
        Assert.AreEqual(HelperManager.CannotSummon, HelperRules.CheckSummon(call with { Summoned = true, HelpersInMaze = 3 }));

        foreach (var (members, room) in new[] { (1, 3), (2, 2), (4, 0) })
        {
            for (var helpers = 0; helpers <= room; helpers++)
            {
                var expected = helpers < room ? HelperRules.Allowed : HelperRules.Full;

                Assert.AreEqual(expected, HelperRules.CheckSummon(call with { HelpersInMaze = helpers, PartyMembers = members }), $"{helpers} helpers and {members} people");
            }
        }

        Assert.AreEqual(HelperManager.NotInMaze, HelperRules.CheckRelease(true, false, false));
        Assert.AreEqual(HelperManager.CannotSummon, HelperRules.CheckRelease(true, true, false));
        Assert.IsNull(HelperRules.CheckRelease(false, true, true));
        Assert.AreEqual(HelperRules.Allowed, HelperRules.CheckRelease(true, true, true));
    }

    /// <summary>
    /// A monster that comes into the world (04/15, the message of a called helper) is a byte count and then each monster as
    /// in the list of the monsters already there (04/23), which counts with a short.
    /// </summary>
    [TestMethod]
    public void AMonsterThatComesInIsWrittenAsInTheListOfThoseThere()
    {
        var monster = new STMonsterInfo
        {
            stNpcInfo = new STNpcInfo
            {
                uxActorID = new ActorComponent(7, ActorType.Monster),
                stPosInfo = new Messages.Response.Person.WorldValue(1, default, new Vector3(1, 2, 3), 90f),
                nHP = 500,
                nWayPointID = 0,
                nSectorID = 0,
                byLevel = 20,
                nTableID = Mirium
            },
            uxParentActorID = new ActorComponent(1, ActorType.User),
            nSpawnBoxID = 0,
            bBattlePos = false,
            fCurSuperArmor = 0f,
            fMaxSuperArmor = 0f,
            vecStat = []
        };

        var coming = Written(new WorldInInfoMonsterResponse { Monsters = [monster] });
        var there = Written(new WorldOtherInfosMonsterResponse { Monsters = [monster] });

        Assert.AreEqual((byte)1, coming[0]);
        CollectionAssert.AreEqual(there[2..], coming[1..]);
    }

    private static byte[] Written(Messages.Abstractions.IResponseHotspotMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    /// <summary>
    /// A called helper appears to every player of the maze (04/15) with its owner as its parent, then everybody hears whose
    /// it is (27/02). When its owner leaves the maze it falls and everybody hears it was sent away; nothing of it stays.
    /// </summary>
    [TestMethod]
    public async Task ACalledHelperIsShownToEveryPlayerAndGoesWhenItsOwnerLeaves()
    {
        var world = World.Create();

        try
        {
            RecordingConnection first = new(), second = new();
            var owner = Player(world, 1, first);
            var member = Player(world, 2, second);
            var batch = TestMazes.Map();
            var maze = TestMazes.Create(world, owner, batch: batch, creatures: Creatures(world, batch, out var stats));

            Assert.IsTrue(maze.Join(member));

            var helper = maze.SummonHelper(owner, stats.Combat(Mirium, 20, []), new HelperValue { Helper = Mirium, Summoned = true });

            Assert.IsNotNull(helper);
            Assert.AreEqual(1, maze.Helpers.Count);

            foreach (var connection in new[] { first, second })
            {
                // The frames reach a connection through the queue of its writer: wait for the one that comes last.
                await MetricAssertions.UntilAsync(
                    () => Received(connection).Any(e => e.Is(GroupOpcode.Helper, (byte)HelperOpcode.Summon)),
                    "The player is told whose helper it is.");

                var frames = Received(connection);
                var shown = Array.FindIndex(frames, e => e.Is(GroupOpcode.World, (byte)WorldOpcode.InInfoMonster));
                var named = Array.FindIndex(frames, e => e.Is(GroupOpcode.Helper, (byte)HelperOpcode.Summon));

                Assert.IsTrue(shown >= 0 && named > shown, "Every player sees the helper appear, then whose it is.");
                Assert.AreEqual(1, frames[named].Body[8], "Called.");
                Assert.AreEqual(1, BitConverter.ToInt32(frames[named].Body, 0), "The owner is named.");
            }

            Assert.IsFalse(maze.Leave(owner), "The maze goes on for the other player.");

            Assert.AreEqual(0, maze.Helpers.Count);
            Assert.IsFalse(world.IsAlive(helper.Entity));

            await MetricAssertions.UntilAsync(
                () => Received(second).Count(e => e.Is(GroupOpcode.Helper, (byte)HelperOpcode.Summon)) == 2,
                "The other player is told the helper was sent away.");

            var after = Received(second);
            var sent = after.Last(e => e.Is(GroupOpcode.Helper, (byte)HelperOpcode.Summon));

            Assert.AreEqual(0, sent.Body[8], "The other player hears the helper was sent away.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A monster a helper kills is its owner's kill: what pays for a kill (drops, experience, quests) goes to the owner.
    /// </summary>
    [TestMethod]
    public async Task AMonsterKilledByAHelperIsItsOwnersKill()
    {
        var world = World.Create();

        try
        {
            var owner = Player(world, 1, new RecordingConnection());
            var monster = TestTables.Load<MonsterRow>("tb_Monster").First(e => e.Type == 0 && e.Level is > 1 and < 20 && e.Faction != 10);
            var quests = TestMazes.Quests(world);
            var batch = TestMazes.Map(PartyMazeTest.MapWithMonster(monster.Id));
            var maze = TestMazes.Create(world, owner, batch: batch, creatures: Creatures(world, batch, out var stats), quests: quests,
                reports: new MazeReporter(null!, null!, null!, null!, quests, NullLogger<MazeReporter>.Instance));
            var killed = new List<(Entity Killer, int Monster)>();

            maze.MonsterKilled += (killer, identifier, _) => killed.Add((killer, identifier));
            maze.SpawnSector(1);

            var target = maze.Creatures.FindMonsters((_, _) => true).Single();

            world.Set(target, world.Get<CreatureComponent>(target) with { Health = 1 });

            var helper = maze.SummonHelper(owner, stats.Combat(Mirium, 20, []), new HelperValue { Helper = Mirium, Summoned = true })!;
            var blow = new AttackTrigger(1, 0, 0, 0, 0f, 360f, Vector3.Zero, 0, 0, 100, 100, 0);

            maze.Helpers.Queue(new HelperStrike(helper, 0, blow, [target]));
            maze.Creatures.Step(TimeSpan.FromMilliseconds(50));

            await MetricAssertions.UntilAsync(() => killed.Count == 1, "The monster dies.");

            Assert.AreEqual(owner, killed[0].Killer, "The owner killed it.");
            Assert.AreEqual(monster.Id, killed[0].Monster);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A helper that stands in the maze fights with what it wears now: a weapon put on raises its attacks, taken off (and
    /// free to be sold) it is no longer in its blows, and the orb of a friend counts until the maze is cleared. Every record
    /// the client gets of it meanwhile (27/10, 27/09, 27/11) says it is called, as the window must keep offering to send it
    /// away; once sent away it is not called any more.
    /// </summary>
    [TestMethod]
    public async Task AHelperInTheMazeFightsWithWhatItWearsNowAndIsToldAsCalled()
    {
        const int weaponOfMirium = 360010102;
        const float weaponAttack = 157f;
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var owner = Player(world, 1, connection);
            var catalog = new ItemCatalog(
                TestTables.Load<ItemRow>("tb_Item"),
                TestTables.Load<ItemClassifyRow>("tb_Item_Classify"),
                TestTables.Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                TestTables.Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));
            var serials = new LocalItemSerials();
            var inventory = new InventoryService(world, catalog, serials);
            var helpers = new HelperCatalog(TestTables.Load<HelperRow>("tb_Helper"));
            var stats = new HelperStats(helpers, TestTables.Load<MonsterRow>("tb_Monster"), TestTables.Load<StatusRow>("tb_Status"), catalog);
            var gear = new HelperGear(world, helpers, inventory, stats, new MazeSummonedHelpers(world, stats));
            var book = new HelperBook();

            Assert.IsTrue(book.TryAdd(Mirium, out var entry));

            world.Add(owner,
                new InventoryComponent(new InventoryFactory(catalog, TestTables.Load<CreateClothRow>("tb_Create_Cloth"), serials).CreateNew(Person(1))),
                new HelperComponent { Person = 1, Book = book });

            var batch = TestMazes.Map();
            var maze = TestMazes.Create(world, owner, batch: batch, creatures: Creatures(world, batch, out _));

            world.Add(owner, maze);

            var record = gear.Record(owner, entry, summoned: true);
            var called = maze.SummonHelper(owner, MazeSummonedHelpers.CombatOf(world, stats, owner, record), record)!;

            float Attack() => world.Get<MonsterCombatComponent>(called.Entity).Stats.Physical.AttackMax;

            var own = Attack();

            Assert.IsTrue(inventory.TryGive(owner, weaponOfMirium, 1, out var given));

            var weapon = inventory.InventoryOf(owner)!.At(given[0].Storage, given[0].Slot)!;

            gear.Equip(owner, HelperGearTest.Request(Mirium, weapon, null, 0));

            Assert.AreEqual(own + weaponAttack, Attack(), "the weapon put on is in its blows at once");
            Assert.AreEqual(1, await CalledByteAsync(connection, HelperOpcode.Equip, gear.Record(owner, entry)), "27/10 says it is called");

            gear.Equip(owner, HelperGearTest.Request(Mirium, null, weapon, 0));

            Assert.AreEqual(StorageGroup.Common, weapon.Storage, "the weapon is back in the bag");
            Assert.AreEqual(own, Attack(), "taken off, the weapon is no longer in its blows");

            var orb = new HelperOrb { Friend = 9, SupportType = HelperStats.AttackOrb, Value = 30f, Until = DateTimeOffset.UtcNow.AddHours(24) };

            Assert.IsTrue(gear.WearOrb(owner, Mirium, orb, out var withOrb));
            Assert.IsTrue(withOrb.Summoned, "27/09 says it is called");
            Assert.AreEqual(own + 30f, Attack(), "the orb counts at once");

            var before = connection.Frames.Length;

            gear.OnMazeCleared(owner);

            Assert.AreEqual(own, Attack(), "the orb given back at the clear no longer counts");
            Assert.AreEqual(1, await CalledByteAsync(connection, HelperOpcode.UpdateInfo, gear.Record(owner, entry), before), "27/11 says it is called");

            Assert.IsTrue(maze.ReleaseHelper(owner, Mirium));
            Assert.IsFalse(gear.Record(owner, entry).Summoned, "sent away, it is not called any more");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// The "called" byte of the last record of a helper the client got in this message: the record ends with it, the two
    /// lists of stats (a count, then a byte and a float each) and the place in the list.
    /// </summary>
    private static async Task<byte> CalledByteAsync(RecordingConnection connection, HelperOpcode message, HelperValue record, int after = 0)
    {
        await MetricAssertions.UntilAsync(
            () => Received(connection).Skip(after).Any(e => e.Is(GroupOpcode.Helper, (byte)message)),
            "The client is told of the helper.");

        var body = Received(connection).Skip(after).Last(e => e.Is(GroupOpcode.Helper, (byte)message)).Body;

        return body[^(1 + 2 + 5 * record.Additional.Count + 2 + 5 * record.Origin.Count + 1)];
    }

    private static PersonPersistent Person(int id) => new()
    {
        Id = id,
        Name = "Member" + id,
        Hero = Hero.Haru,
        DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
        EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
        Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
        Account = null!, Gate = null!
    };

    /// <summary>
    /// The creatures of a maze that knows the monsters of the tables and the helpers among them.
    /// </summary>
    private static LuaCreatureManager Creatures(World world, OpenWorker.Batch.VBatchFile batch, out HelperStats stats)
    {
        var monsters = TestTables.Load<MonsterRow>("tb_Monster");
        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var statuses = TestTables.Load<StatusRow>("tb_Status");
        var combat = new CombatTables(monsters, statuses, skills, TestTables.Load<CharacterInfoRow>("tb_Character_Info"));
        var scripts = Environment.GetEnvironmentVariable("OPENWORKER_TEST_AI") ?? Path.Join(Path.GetTempPath(), "no-scripts");
        var services = new CreatureServices(
            new AiScriptLoader(NullLogger<AiScriptLoader>.Instance, scripts),
            new MonsterBehaviorTables(monsters, skills, TestTables.Load<FactionRow>("tb_Faction")),
            new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance),
            TestMazes.Clock(),
            new NavMeshProvider());

        stats = new HelperStats(HelperCatalog.From([Mirium]), monsters, statuses);

        return new LuaCreatureManager(world, batch, combat, services);
    }

    private static Entity Player(World world, int person, RecordingConnection connection)
    {
        var life = new AbilityComponentEntry { Current = 1000, Max = 1000 };

        return world.Create(
            new ActorComponent(person, ActorType.User),
            new ServerSessionComponent(connection),
            new WorldComponent { Location = 0, Position = new Vector3(40000, 23000, 150), Rotation = 0f, Jump = 0 },
            new LevelComponent(20, 0),
            new PersonInfoComponent { Name = "Member" + person, Hero = Hero.Haru },
            new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
    }

    private static PlayerFrame[] Received(RecordingConnection connection) => connection.Frames.Select(PlayerFrame.Open).ToArray();
}
