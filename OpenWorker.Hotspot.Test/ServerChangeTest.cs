using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Channel;
using OpenWorker.DistrictServer.Services;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.World.Requests;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A player told to change servers asks for nothing more: the client sends a second way out by itself.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ServerChangeTest
{
    private const short Other = 21311;

    private static WorldEnterRequest Enter()
    {
        var body = new byte[4 + 4 + 4 + 2 + 2 + 4 + 4 + 8 + 2 + 1];

        BinaryPrimitives.WriteInt16LittleEndian(body.AsSpan(12), Other);

        return MazeRequestGuardTest.Read(body, (ref PacketReader r) => WorldEnterRequest.Read(ref r));
    }

    /// <summary>
    /// In a maze, the way out and a move to another map of a player that was told where to go get no answer and reserve
    /// nothing (the service has neither a prologue nor a world manager: reaching them would throw); once its reservation has
    /// ended, it is answered again.
    /// </summary>
    [TestMethod]
    public async Task APlayerToldToChangeServersIsAnsweredNothingInAMazeUntilItsReservationEnds()
    {
        var world = World.Create();

        try
        {
            var time = new FakeTimeProvider();
            var changes = new ServerChanges(world, time);
            var mazes = new ReadOnlyCollection<MazeInfoRow>([MazeResultsTest.MazeRow(Other, level: 30, fatigue: 0)]);
            var fatigue = new FatigueService(world, mazes, TimeProvider.System, Options.Create(new DailyMissionOptions()), NullLogger<FatigueService>.Instance);
            var progress = new SharePointManager(world, new PersonStateStore(null!), SharePointCatalog.Of([]), null!, NullLogger<SharePointManager>.Instance);
            var admission = new MazeAdmission(world, mazes, fatigue, MazeResultsTest.Scoring(MazeResultsTest.None<MazePlayScoreRow>(), MazeResultsTest.None<MazeRuleRow>()), progress);
            var service = MazeRequestGuardTest.Service(world, admission: admission, changes: changes);
            var connection = new RecordingConnection();
            var player = world.Create(new LevelComponent(1, 0), new ServerSessionComponent(connection));
            var context = MazeRequestGuardTest.Context(player);

            changes.Begin(player);

            await service.OnHandleAsync(context, MazeRequestGuardTest.Read(new byte[4 + 8 + 4 + 4 + 4 + 1], (ref PacketReader r) => MazeExitRequest.Read(ref r)));
            await service.OnHandleAsync(context, Enter());

            time.Advance(WorldManager.ReservationLifetime);

            await service.OnHandleAsync(context, Enter());

            await MetricAssertions.UntilAsync(() => connection.Frames.Length > 0, "The player is answered once its reservation has ended.");

            Assert.HasCount(1, connection.Frames);
            Assert.IsTrue(PlayerFrame.Open(connection.Frames[0]).Is(GroupOpcode.World, (byte)WorldOpcode.EnterReq | 0x80), "The only answer is the refusal after the reservation.");
        }

        finally
        {
            World.Destroy(world);
        }
    }

    /// <summary>
    /// In a district, a move to another map and a maze entry of a player that was told where to go get no answer (the service
    /// has nothing to check or send with: reaching that would throw).
    /// </summary>
    [TestMethod]
    public async Task APlayerToldToChangeServersIsAnsweredNothingInADistrict()
    {
        var world = World.Create();

        try
        {
            var changes = new ServerChanges(world, TimeProvider.System);
            var service = new WorldService(world, null!, null!, null!, null!, null!, changes);
            var connection = new RecordingConnection();
            var player = world.Create(new ServerSessionComponent(connection));
            var context = MazeRequestGuardTest.Context(player);

            changes.Begin(player);

            await service.OnHandleAsync(context, Enter());
            await service.OnHandleAsync(context, MazeRequestGuardTest.Read(new byte[4 + 4 + 4 + 2 + 2 + 4 + 4 + 2 + 8 + 1], (ref PacketReader r) => MazeCreateRequest.Read(ref r)));

            Assert.IsEmpty(connection.Frames);
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
