using System.Globalization;
using System.Numerics;
using System.Text;
using Arch.Core;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The open maze of the world (District 6): everybody of a channel shares one instance, and its monsters come back after the time
/// of their box. The Redis rule needs Redis: set OPENWORKER_TEST_REDIS.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class WorldMazeTest
{
    private const short Gate = 1;
    private const short District6 = 30021;

    private static readonly (Guid Server, string Host, short Port) Unregistered = (Guid.Empty, "maze", 100);

    private static ConnectionMultiplexer? _redis;

    private static async Task<IConnectionMultiplexer> RedisAsync()
    {
        var connection = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(connection))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        return _redis ??= await ConnectionMultiplexer.ConnectAsync(new RedisOptions { ConnectionString = connection }.ToConfigurationOptions());
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _redis?.Dispose();
        _redis = null;
    }

    /// <summary>
    /// The first player of a channel opens the instance, the next ones of the channel are sent into it until it is full; the
    /// record follows the instance that holds it, and the next player after it ended opens a new one.
    /// </summary>
    [TestMethod]
    public async Task EverybodyOfAChannelIsSentIntoOneInstanceUntilItIsFull()
    {
        var redis = await RedisAsync();
        var directory = new WorldMazeDirectory(redis);
        var channel = PartyTest.People(1)[0];

        var first = await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2);
        var second = await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2);

        Assert.IsNotNull(first);
        Assert.AreEqual(first.Value.Instance, second?.Instance, "the second player of the channel is sent where the first is");
        Assert.AreEqual(2, second?.Players);
        Assert.IsNull(await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2), "a full instance takes nobody");

        var other = await directory.RouteAsync(Gate, District6, channel + 1, Unregistered, capacity: 2);

        Assert.AreNotEqual(first.Value.Instance, other?.Instance, "another channel has an instance of its own");

        Assert.IsFalse(await directory.HoldAsync(Gate, District6, channel, Guid.NewGuid(), Unregistered, 0), "another instance does not take the record");
        Assert.IsTrue(await directory.HoldAsync(Gate, District6, channel, first.Value.Instance, Unregistered, 1), "the instance renews its record");
        Assert.AreEqual(first.Value.Instance, (await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2))?.Instance,
            "the renewed count lets one more in");

        await directory.ReleaseAsync(Gate, District6, channel, Guid.NewGuid());

        Assert.IsNull(await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2),
            "the release of another instance leaves the record, full again");

        await directory.ReleaseAsync(Gate, District6, channel, first.Value.Instance);

        Assert.AreNotEqual(first.Value.Instance, (await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2))?.Instance,
            "the next player after the instance ended opens a new one");

        await directory.ReleaseAsync(Gate, District6, channel + 1, other!.Value.Instance);
    }

    private const int Sector = 20001;
    private const int Returning = 101;
    private const int Staying = 102;
    private const int Monster = 5000;

    private static string Vector(Vector3 v) => string.Create(CultureInfo.InvariantCulture, $"{v.X:0.000},{v.Y:0.000},{v.Z:0.000}");

    private static string Basic(int id, Vector3 low, Vector3 high) =>
        $"""<m_ID type="0" value="{id}" /> <m_iGenerateID type="0" value="{id}" /> <iLayerBitmask type="4" value="Common" /> <m_vPosTopLeft type="7" value="{Vector(low)}" /> <m_vPosBottomRight type="7" value="{Vector(high)}" /> <m_fRotation type="2" value="0.0000" /> <m_vSize type="7" value="{Vector(high - low)}" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" />""";

    private static string SectorBox() =>
        $"""<VSectorBox boxtype="eEventBoxType_Sector"> {Basic(Sector, Vector3.Zero, new Vector3(100000f))} <m_eType type="4" value="General" /> <m_szEnterLua type="12" value="" /> <m_eClearType type="4" value="None" /> <m_iClearKillRatio type="0" value="0" /> <m_szClearLua type="12" value="" /> <m_iSectorTitle type="0" value="0" /> <m_iSectorExitType type="4" value="Portal" /> <m_iSectorExitID type="0" value="0" /> <m_iRelativeSectorID type="0" value="0" /> <m_iConditionKillRatio_1Step type="0" value="0" /> <m_iConditionKillRatio_2Step type="0" value="0" /> <m_iConditionKillRatio_3Step type="0" value="0" /> <m_iConditionKillRatio_4Step type="0" value="0" /> <m_iConditionKillRatio_5Step type="0" value="0" /> </VSectorBox>""";

    /// <summary>
    /// A box that brings one monster in at once, with the respawn time given in milliseconds.
    /// </summary>
    private static string SpawnBox(int id, float respawn)
    {
        var text = new StringBuilder($"""<VMonsterSpawnBox boxtype="eEventBoxType_MonsterSpawn"> {Basic(id, new Vector3(500f, 500f, 0f), new Vector3(600f, 600f, 100f))} """);

        for (var slot = 1; slot <= 10; slot++)
        {
            var monster = slot == 1 ? Monster : 0;

            text.Append(CultureInfo.InvariantCulture, $"""<m_eType{slot} type="4" value="Monster" /> <m_iMonsterID{slot} type="0" value="{monster}" /> <m_iChance{slot} type="0" value="{(monster == 0 ? 0 : 10000)}" /> """);
        }

        text.Append(CultureInfo.InvariantCulture, $"""<m_eCreationPositionType type="4" value="Center" /> <m_eMoveType type="4" value="Free" /> <m_eCreationCondition type="4" value="Immediate" /> <m_CreationEffectFile type="12" value="" /> <m_fWaitCreationDelayTime type="2" value="0.0000" /> <m_fWaitCreationSequenceTime type="2" value="0.0000" /> <m_iWaitCreationMaxWave type="0" value="1" /> <m_iMaxEntityCount type="0" value="1" /> <m_eWaitCreationSequenceType type="4" value="All" /> <m_iWaypoint type="0" value="0" /> <m_iAggroGroupID type="0" value="0" /> <m_iAggroDistance type="0" value="0" /> <m_iAggroMaxCount type="0" value="0" /> <m_fLookRatio type="2" value="1.0000" /> <m_bShowSight type="5" value="FALSE" /> <m_szObjectKey type="12" value="" /> <m_eScriptType type="4" value="None" /> <m_iCheckScriptHP1 type="0" value="0" /> <m_iCheckScriptHP2 type="0" value="0" /> <m_iCheckScriptHP3 type="0" value="0" /> <m_iCheckScriptHP4 type="0" value="0" /> <m_iCheckScriptHP5 type="0" value="0" /> <m_iSectorID type="0" value="{Sector}" /> <m_ChangeSpawnAction type="12" value="" /> <m_ProtectionTarget type="0" value="0" /> <m_RespawnTime type="2" value="{respawn:0.0000}" /> <m_iStep type="0" value="1" /> <m_eRespawnType type="4" value="None" /> <m_iRespawnCondition type="0" value="0" /> <m_iGroupID type="0" value="0" /> </VMonsterSpawnBox>""");

        return text.ToString();
    }

    private static string Map() =>
        "<root><Batchs eventtype=\"EventBox\">" + SectorBox() + SpawnBox(Returning, 1000f) + SpawnBox(Staying, 0f) +
        "</Batchs><Batchs eventtype=\"EventPoint\"/></root>";

    /// <summary>
    /// What the map brings comes out as the first player arrives; a monster of a box with a respawn time comes back that many
    /// milliseconds of game time after it falls, one of a box without stays dead, and the sector never clears.
    /// </summary>
    [TestMethod]
    public async Task AMonsterOfAnOpenMazeComesBackAfterTheTimeOfItsBox()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var clock = TestMazes.Clock(time);
            var player = world.Create(
                new ActorComponent(1, ActorType.User),
                new ServerSessionComponent(new RecordingConnection()),
                new WorldComponent { Location = 0, Position = new Vector3(100f, 100f, 0f), Rotation = 0f, Jump = 0 });
            var maze = TestMazes.Create(world, player, clock: clock, map: Map(), channel: 1);

            int Living() => maze.Creatures.FindMonsters((_, _) => true).Length;

            void Run(TimeSpan span)
            {
                for (var passed = TimeSpan.Zero; passed < span; passed += SimulationClock.Period)
                {
                    time.Advance(SimulationClock.Period);
                    clock.Tick();
                }
            }

            Assert.IsTrue(maze.TryStart());
            Assert.AreEqual(2, Living(), "every box brings its monster at once");

            await maze.ClearSectorAsync(Sector);

            Assert.AreEqual(0, Living());
            Assert.AreEqual(0, maze.Progress.Cleared.Count, "nothing clears in an open maze");
            Assert.IsFalse(maze.IsComplete);

            Run(TimeSpan.FromMilliseconds(900));

            Assert.AreEqual(0, Living(), "the monster is not back before its time");

            Run(TimeSpan.FromMilliseconds(200));

            var back = maze.Creatures.FindMonsters((_, _) => true);

            Assert.HasCount(1, back, "the monster of the box with a respawn time is back, the other stays dead");
            Assert.AreEqual(Monster, world.Get<CreatureComponent>(back[0]).Identifier);
            Assert.AreEqual(Returning, world.Get<NonPlayableCreatureComponent>(back[0]).SpawnBox);

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
