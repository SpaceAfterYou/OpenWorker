using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a maze tells its players reaches those still there, and a sync its script leaves in a sector reaches a player
/// who loads the maze later.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeTellingTest
{
    private const int Sector = 10;

    private static string Vector(float x, float y, float z) => string.Create(CultureInfo.InvariantCulture, $"{x:0.000},{y:0.000},{z:0.000}");

    private static string Map() =>
        $"""<root><Batchs eventtype="EventBox"><VSectorBox boxtype="eEventBoxType_Sector"> <m_ID type="0" value="{Sector}" /> <m_iGenerateID type="0" value="{Sector}" /> <iLayerBitmask type="4" value="Common" /> <m_vPosTopLeft type="7" value="{Vector(0f, 0f, 0f)}" /> <m_vPosBottomRight type="7" value="{Vector(1000f, 1000f, 1000f)}" /> <m_fRotation type="2" value="0.0000" /> <m_vSize type="7" value="{Vector(1000f, 1000f, 1000f)}" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" /> <m_eType type="4" value="General" /> <m_szEnterLua type="12" value="" /> <m_eClearType type="4" value="None" /> <m_iClearKillRatio type="0" value="0" /> <m_szClearLua type="12" value="" /> <m_iSectorTitle type="0" value="0" /> <m_iSectorExitType type="4" value="Portal" /> <m_iSectorExitID type="0" value="0" /> <m_iRelativeSectorID type="0" value="0" /> <m_iConditionKillRatio_1Step type="0" value="0" /> <m_iConditionKillRatio_2Step type="0" value="0" /> <m_iConditionKillRatio_3Step type="0" value="0" /> <m_iConditionKillRatio_4Step type="0" value="0" /> <m_iConditionKillRatio_5Step type="0" value="0" /> </VSectorBox></Batchs><Batchs eventtype="EventPoint"/></root>""";

    private static Entity Player(World world, int person, RecordingConnection connection)
    {
        return world.Create(
            new ActorComponent(person, ActorType.User),
            new ServerSessionComponent(connection),
            new WorldComponent { Location = 0, Position = new Vector3(100f, 100f, 0f), Rotation = 0f, Jump = 0 });
    }

    private static PlayerFrame[] Received(RecordingConnection connection, MessageOpcode opcode) =>
        connection.Frames.Select(PlayerFrame.Open).Where(e => e.Opcode.Value == opcode.Value).ToArray();

    [TestMethod]
    public async Task AClearWithAPlayerGoneTellsTheOthers()
    {
        var world = World.Create();

        try
        {
            var first = new RecordingConnection();
            var last = new RecordingConnection();
            var master = Player(world, 1, first);
            var gone = Player(world, 2, new RecordingConnection());
            var member = Player(world, 3, last);
            var maze = TestMazes.Create(world, master);

            Assert.IsTrue(maze.Join(gone));
            Assert.IsTrue(maze.Join(member));

            world.Destroy(gone);

            maze.SendClearInfo();

            var clear = new MazeClearInfoResponse().Opcode;

            await MetricAssertions.UntilAsync(() => Received(first, clear).Length == 1 && Received(last, clear).Length == 1, "Both players still there hear the clear.");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }

    [TestMethod]
    public async Task ASyncKeptInASectorReachesALaterPlayerUntilItsTimeIsUp()
    {
        var world = World.Create();
        var time = new FakeTimeProvider();
        var clock = TestMazes.Clock(time);

        try
        {
            var master = Player(world, 1, new RecordingConnection());
            var later = new RecordingConnection();
            var member = Player(world, 2, later);
            var maze = TestMazes.Create(world, master, map: Map(), clock: clock);

            maze.LuaClientSync(0, 5, Sector);
            maze.LuaClientSync(0, 6, Sector, 2f);
            maze.LuaClientSync(0, 7);

            for (var elapsed = TimeSpan.Zero; elapsed < TimeSpan.FromSeconds(3); elapsed += SimulationClock.Period)
            {
                time.Advance(SimulationClock.Period);
                clock.Tick();
            }

            Assert.IsTrue(maze.Join(member));

            maze.ShowClientSyncsTo(member);

            var sync = new MazeLuaClientSyncResponse().Opcode;

            await MetricAssertions.UntilAsync(() => Received(later, sync).Length >= 1, "The later player hears the kept sync.");

            CollectionAssert.AreEqual(new[] { 5 }, Received(later, sync).Select(e => BinaryPrimitives.ReadInt32LittleEndian(e.Body.AsSpan(4))).ToArray(),
                "Only the sync kept for good is heard again: the timed one is over and the other named no sector.");

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
