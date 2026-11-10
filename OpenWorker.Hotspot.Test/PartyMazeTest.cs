using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Lua;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A party plays one maze instance: it is shared, kept for the rest when a member leaves and ended by the last one out,
/// joined where it stands, and a player alone has one of their own as before.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PartyMazeTest
{
    private const int Sector = 1;

    /// <summary>
    /// What each of two members gets: 100 for the monster, times 0.9 and 0.5.
    /// </summary>
    private static readonly int[] Shared = [45, 45];
    private const int Box = 7;

    /// <summary>
    /// A map with one box of the first sector that shows one monster at once.
    /// </summary>
    internal static string MapWithMonster(int monster) =>
        "<root><Batchs eventtype=\"EventBox\">" + SpawnBox.Replace("{MONSTER}", monster.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal) +
        "</Batchs><Batchs eventtype=\"EventPoint\"/></root>";

    private const string SpawnBox = """<VMonsterSpawnBox boxtype="eEventBoxType_MonsterSpawn"> <m_ID type="0" value="7" /> <m_iGenerateID type="0" value="140000" /> <iLayerBitmask type="4" value="Normal" /> <m_vPosTopLeft type="7" value="39846.422,22910.063,103.000" /> <m_vPosBottomRight type="7" value="40146.422,23210.063,203.000" /> <m_fRotation type="2" value="0.0000" /> <m_vSize type="7" value="300.000,300.000,100.000" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" /> <m_eType1 type="4" value="Monster" /> <m_iMonsterID1 type="0" value="{MONSTER}" /> <m_iChance1 type="0" value="2000" /> <m_eType2 type="4" value="Monster" /> <m_iMonsterID2 type="0" value="0" /> <m_iChance2 type="0" value="0" /> <m_eType3 type="4" value="Monster" /> <m_iMonsterID3 type="0" value="0" /> <m_iChance3 type="0" value="0" /> <m_eType4 type="4" value="Monster" /> <m_iMonsterID4 type="0" value="0" /> <m_iChance4 type="0" value="10000" /> <m_eType5 type="4" value="Monster" /> <m_iMonsterID5 type="0" value="0" /> <m_iChance5 type="0" value="0" /> <m_eType6 type="4" value="Monster" /> <m_iMonsterID6 type="0" value="0" /> <m_iChance6 type="0" value="0" /> <m_eType7 type="4" value="Monster" /> <m_iMonsterID7 type="0" value="0" /> <m_iChance7 type="0" value="10000" /> <m_eType8 type="4" value="Monster" /> <m_iMonsterID8 type="0" value="0" /> <m_iChance8 type="0" value="0" /> <m_eType9 type="4" value="Monster" /> <m_iMonsterID9 type="0" value="0" /> <m_iChance9 type="0" value="0" /> <m_eType10 type="4" value="Monster" /> <m_iMonsterID10 type="0" value="0" /> <m_iChance10 type="0" value="10000" /> <m_eCreationPositionType type="4" value="Random" /> <m_eMoveType type="4" value="Free" /> <m_eCreationCondition type="4" value="Immediate" /> <m_CreationEffectFile type="12" value="" /> <m_fWaitCreationDelayTime type="2" value="0.0000" /> <m_fWaitCreationSequenceTime type="2" value="0.0000" /> <m_iWaitCreationMaxWave type="0" value="0" /> <m_iMaxEntityCount type="0" value="0" /> <m_eWaitCreationSequenceType type="4" value="All" /> <m_iWaypoint type="0" value="0" /> <m_iAggroGroupID type="0" value="0" /> <m_iAggroDistance type="0" value="0" /> <m_iAggroMaxCount type="0" value="0" /> <m_fLookRatio type="2" value="3.0000" /> <m_bShowSight type="5" value="FALSE" /> <m_szObjectKey type="12" value="" /> <m_eScriptType type="4" value="None" /> <m_iCheckScriptHP1 type="0" value="0" /> <m_iCheckScriptHP2 type="0" value="0" /> <m_iCheckScriptHP3 type="0" value="0" /> <m_iCheckScriptHP4 type="0" value="0" /> <m_iCheckScriptHP5 type="0" value="0" /> <m_iSectorID type="0" value="1" /> <m_ChangeSpawnAction type="12" value="" /> <m_ProtectionTarget type="0" value="0" /> <m_RespawnTime type="2" value="1.0000" /> <m_iStep type="0" value="1" /> <m_eRespawnType type="4" value="None" /> <m_iRespawnCondition type="0" value="0" /> <m_iGroupID type="0" value="0" /> </VMonsterSpawnBox>""";

    private static Entity Player(World world, int person, byte level, RecordingConnection connection)
    {
        var life = new AbilityComponentEntry { Current = 100, Max = 100 };

        return world.Create(
            new ActorComponent(person, ActorType.User),
            new ServerSessionComponent(connection),
            new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
            new LevelComponent(level, 0),
            new PersonInfoComponent { Name = "Member" + person, Hero = Hero.Haru },
            new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } });
    }

    private static PlayerFrame[] Received(RecordingConnection connection) => connection.Frames.Select(PlayerFrame.Open).ToArray();

    private static async Task UntilAsync(Func<bool> condition, string what)
    {
        await MetricAssertions.UntilAsync(condition, what);
    }

    private static ReadOnlyCollection<T> Rows<T>(params Action<BinaryWriter>[] rows)
    {
        return new ReadOnlyCollection<T>(rows.Select(write =>
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            write(writer);
            writer.Flush();
            stream.Position = 0;

            return (T)Activator.CreateInstance(typeof(T), new BinaryReader(stream))!;
        }).ToArray());
    }

    /// <summary>
    /// Two members in one instance see the same monster die, once each, and both get the whole experience of the kill
    /// times the party rates: the one of how far apart their levels are, and the one of how far the highest is above the
    /// monster.
    /// </summary>
    [TestMethod]
    public async Task TwoMembersOfOneInstanceSeeAMonsterDieOnceAndBothGetThePartyExperience()
    {
        var monster = TestTables.Load<MonsterRow>("tb_Monster").First(e => e.Level is > 1 and < 20 && e.Type == 0 && e.Rank <= 6);
        var world = World.Create();

        try
        {
            RecordingConnection first = new(), second = new();
            var master = Player(world, 1, 20, first);
            var member = Player(world, 2, 15, second);
            var maze = TestMazes.Create(world, master, map: MapWithMonster(monster.Id));

            Assert.IsTrue(maze.Join(member));
            maze.SpawnSector(Sector);

            Assert.HasCount(1, maze.Creatures.FindMonsters((_, _) => true), "One monster for the whole party");

            await maze.ClearSectorAsync(Sector);

            var died = new MonsterDieResponse().Opcode;

            await UntilAsync(() => Received(first).Count(e => e.Opcode.Value == died.Value) == 1 && Received(second).Count(e => e.Opcode.Value == died.Value) == 1, "Each member sees the monster die once.");

            var experience = new ExperienceService(
                world,
                null!,
                TestTables.Load<LevelupPointRow>("tb_Levelup_Point"),
                Rows<MonsterExpRow>(writer =>
                {
                    writer.Write((byte)monster.Level);
                    writer.Write((byte)monster.Level);

                    for (var rank = 0; rank < 7; rank++)
                    {
                        writer.Write(rank == monster.Rank ? 100 : 0);
                    }
                }),
                Rows<PartyExpMobRow>(writer =>
                {
                    writer.Write((sbyte)(20 - monster.Level));
                    writer.Write(0.5f);
                }),
                Rows<MonsterRewardRow>(),
                TestTables.Load<MonsterRow>("tb_Monster"),
                levelGaps: Rows<PartyExpLevelRow>(writer =>
                {
                    writer.Write((byte)5);
                    writer.Write(0.9f);
                }));

            var given = experience.GiveForParty([master, member], monster.Id);

            CollectionAssert.AreEqual(Shared, given, "100 for the monster, times 0.9 for five levels apart, times 0.5 for the highest being above it");
            Assert.AreEqual(45, world.Get<LevelComponent>(master).Experience);
            Assert.AreEqual(45, world.Get<LevelComponent>(member).Experience);

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A member who leaves leaves the instance: the maze goes on for the other, who sees the member go, and the last one out
    /// ends it.
    /// </summary>
    [TestMethod]
    public async Task AMemberLeavingKeepsTheMazeForTheOtherAndTheLastOneOutEndsIt()
    {
        var world = World.Create();

        try
        {
            RecordingConnection first = new(), second = new();
            var master = Player(world, 1, 20, first);
            var member = Player(world, 2, 20, second);
            var instances = new MazeInstances(world, null!, NullLogger<MazeInstances>.Instance);
            var instance = Guid.NewGuid();
            var maze = (await instances.GetOrOpenAsync(instance, () => ValueTask.FromResult<LuaMaze?>(TestMazes.Create(world, master, instance: instance))))!;

            Assert.IsTrue(maze.Join(member));
            world.Add(master, maze);
            world.Add(member, maze);
            maze.MarkLoaded(master);
            maze.MarkLoaded(member);

            await instances.LeaveAsync(master);

            Assert.IsFalse(maze.IsClosed, "The maze goes on for the one who stays");
            Assert.AreEqual(1, maze.MemberCount);
            Assert.IsFalse(world.Has<LuaMaze>(master));
            Assert.AreEqual(1, instances.Count);

            var gone = new Modules.World.Responses.WorldOutInfoPcResponse { Actors = [] }.Opcode;

            await UntilAsync(() => Received(second).Any(e => e.Opcode.Value == gone.Value), "The one who stays sees the other go.");

            await instances.LeaveAsync(member);

            Assert.IsTrue(maze.IsClosed, "The last one out ends the maze");
            Assert.AreEqual(0, instances.Count);
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A member who comes later, or while the first is still opening the maze, gets the instance where it stands: the maze
    /// is opened once.
    /// </summary>
    [TestMethod]
    public async Task ALaterMemberJoinsTheInstanceWhereItStands()
    {
        var world = World.Create();

        try
        {
            var master = Player(world, 1, 20, new RecordingConnection());
            var member = Player(world, 2, 20, new RecordingConnection());
            var late = Player(world, 3, 20, new RecordingConnection());
            var instances = new MazeInstances(world, null!, NullLogger<MazeInstances>.Instance);
            var instance = Guid.NewGuid();
            var opening = new TaskCompletionSource<LuaMaze?>();
            var opened = 0;

            ValueTask<LuaMaze?> Open()
            {
                opened++;

                return new ValueTask<LuaMaze?>(opening.Task);
            }

            var firstArrival = instances.GetOrOpenAsync(instance, Open).AsTask();
            var secondArrival = instances.GetOrOpenAsync(instance, Open).AsTask();

            var maze = TestMazes.Create(world, master, instance: instance);

            maze.SpawnSector(Sector);
            opening.SetResult(maze);

            Assert.AreSame(maze, await firstArrival);
            Assert.AreSame(maze, await secondArrival, "Who arrives while the maze opens waits for the same maze");
            Assert.IsTrue(maze.Join(member));

            maze.Progress.ClearSector(Sector);

            var later = await instances.GetOrOpenAsync(instance, Open);

            Assert.AreSame(maze, later, "Who comes later finds the maze where it stands");
            Assert.IsTrue(later!.Join(late));
            Assert.IsTrue(later.Progress.IsSectorCleared(Sector));
            Assert.AreEqual(1, opened, "The maze is opened once");
            Assert.AreEqual(3, maze.MemberCount);

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A maze opened for four makes its monsters as strong as four members make them; when the party has two members left
    /// they become as strong as two make them, each with the share of life it had, and the members are told.
    /// </summary>
    [TestMethod]
    public async Task WhenTheMembersOfThePartyAreFewerTheMonstersBecomeWeakerAndKeepTheirShareOfLife()
    {
        var monsters = TestTables.Load<MonsterRow>("tb_Monster");
        var statuses = TestTables.Load<StatusRow>("tb_Status");
        var tables = new Gameplay.Combat.CombatTables(monsters, statuses, TestMazes.None<SkillRow>(), TestMazes.None<CharacterInfoRow>());
        var monster = monsters.First(e => e.Level is > 1 and < 20 && e.Type == 0 && e.Rank <= 6 && tables.GetMonster(e.Id).Stats.MaxHealth >= 100f);
        var origin = (int)tables.GetMonster(monster.Id).Stats.MaxHealth;
        var revision = new Gameplay.Modules.Maze.PartyRevision(
            PartyRevisionTest.Rows<MonsterReviseGroupRow>(PartyRevisionTest.Monster((uint)monster.Id, 0, PartyRevisionTest.Group)),
            PartyRevisionTest.Rows<PartyReviseRatesRow>(
                PartyRevisionTest.Rates(1, 1, 1f, 1f, 1f, 20f, 3000),
                PartyRevisionTest.Rates(2, 2, 2f, 1f, 1f, 20f, 3000),
                PartyRevisionTest.Rates(3, 4, 4f, 1f, 1f, 20f, 3000)));
        var world = World.Create();

        try
        {
            RecordingConnection first = new(), second = new();
            var master = Player(world, 1, 20, first);
            var member = Player(world, 2, 20, second);
            var services = new Lua.Ai.CreatureServices(null!, new Gameplay.Ai.MonsterBehaviorTables(TestMazes.None<MonsterRow>(), TestMazes.None<SkillRow>()), null!, TestMazes.Clock(), new Navigation.NavMeshProvider(), revision: revision);
            var batch = TestMazes.Map(MapWithMonster(monster.Id));
            var creatures = new Lua.Managers.LuaCreatureManager(world, batch, tables, services);

            creatures.StartParty(4);

            var maze = TestMazes.Create(world, master, batch: batch, creatures: creatures);

            Assert.IsTrue(maze.Join(member));
            maze.SpawnSector(Sector);

            var body = creatures.FindMonsters((_, _) => true).Single();

            Assert.AreEqual(4 * origin, world.Get<MonsterCombatComponent>(body).Stats.MaxHealth, "Four members make it four times as strong");

            world.Set(body, world.Get<CreatureComponent>(body) with { Health = 2 * origin });

            creatures.ResizeParty(2);

            Assert.AreEqual(2 * origin, world.Get<MonsterCombatComponent>(body).Stats.MaxHealth, "Two members make it twice as strong");
            Assert.AreEqual(origin, world.Get<CreatureComponent>(body).Health, "It keeps the half of its life it had");

            var notice = new Modules.Maze.Responses.MazeNoticeResponse().Opcode;
            var changed = new MonsterUpdateStatListResponse { Stats = [] }.Opcode;

            foreach (var connection in new[] { first, second })
            {
                await UntilAsync(() => Received(connection).Any(e => e.Opcode.Value == notice.Value) && Received(connection).Any(e => e.Opcode.Value == changed.Value), "Every member is told.");
            }

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// A monster that dies in a maze counts for every player in it, and for the killer who has left meanwhile, once each.
    /// </summary>
    [TestMethod]
    public async Task AKillCountsForEveryPlayerInTheMazeOnceEach()
    {
        var world = World.Create();

        try
        {
            var killer = Player(world, 1, 20, new RecordingConnection());
            var member = Player(world, 2, 20, new RecordingConnection());
            var gone = Player(world, 3, 20, new RecordingConnection());
            var reports = new RecordingReporter();
            var maze = TestMazes.Create(world, killer, reports: reports);

            Assert.IsTrue(maze.Join(member));

            await maze.ReportKillAsync(gone, 4242, 7);

            CollectionAssert.AreEquivalent(new[] { killer, member, gone }, reports.Told.Select(e => e.Player).ToArray(), "Everybody in the maze, and the killer who left");
            Assert.IsTrue(reports.Told.All(e => e.Monster == 4242 && e.Conditions == 7));

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    private sealed class RecordingReporter : MazeReporter
    {
        public RecordingReporter() : base(null!, null!, null!, null!, null!, null!)
        {
        }

        public List<(Entity Player, int Monster, uint Conditions)> Told { get; } = [];

        public override ValueTask MonsterKilledAsync(Entity player, int monster, uint conditions)
        {
            Told.Add((player, monster, conditions));

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// A maze with one player in it tells that player alone what happens in it, and ends when the player leaves, as before.
    /// </summary>
    [TestMethod]
    public async Task APlayerAloneHasAMazeOfTheirOwnAsBefore()
    {
        var world = World.Create();

        try
        {
            var connection = new RecordingConnection();
            var player = Player(world, 1, 20, connection);
            var maze = TestMazes.Create(world, player, map: MapWithMonster(1000));

            maze.SpawnSector(Sector);

            Assert.AreEqual(1, maze.MemberCount);
            CollectionAssert.AreEqual(new[] { player }, maze.Members.ToArray());

            await maze.ClearSectorAsync(Sector);

            var died = new MonsterDieResponse().Opcode;

            await UntilAsync(() => Received(connection).Count(e => e.Opcode.Value == died.Value) == 1, "The player sees the monster die.");

            Assert.IsTrue(maze.Leave(player), "The one player out ends the maze");
            Assert.IsTrue(maze.IsClosed);
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
