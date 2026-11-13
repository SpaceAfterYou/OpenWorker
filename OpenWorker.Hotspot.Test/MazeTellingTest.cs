using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a maze tells its players reaches those still there.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeTellingTest
{
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
}
