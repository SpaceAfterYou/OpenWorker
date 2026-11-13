using System.Numerics;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What a district or a maze answers to a client that has entered it.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class EntryAnswerTest
{
    /// <summary>
    /// A client that was playing and moves to another server reads the answer of the play scene: the place of the player
    /// and its echelon, laid out the way the client reads them. The character record is for the client that comes from the
    /// character selection.
    /// </summary>
    [TestMethod]
    public void AClientThatMovesBetweenServersIsAnsweredWithThePlaceOfThePlayer()
    {
        var world = World.Create();

        try
        {
            var map = new MapValue { Location = 21311, Server = 1 };
            var player = world.Create(
                new WorldComponent { Location = 21311, Position = new Vector3(1f, 2f, 3f), Rotation = 90f, Jump = 0, Map = map },
                new RankComponent(3, 1500));

            var message = PersonSnapshotMapper.CreateEntryResponse(world, player, firstConnect: false);

            using var actual = new MemoryStream();
            using (var writer = new BinaryWriter(actual, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(WireBytes.Of(message));
            }

            using var expected = new MemoryStream();
            using (var writer = new BinaryWriter(expected, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(0);
                writer.Write((byte)0);
                writer.Write((short)21311);
                writer.Write((ulong)map);
                writer.Write(1f);
                writer.Write(2f);
                writer.Write(3f);
                writer.Write(90f);
                writer.Write((byte)3);
                writer.Write(1500);
            }

            Assert.AreEqual((byte)GroupOpcode.Character, message.Opcode.Group);
            Assert.AreEqual((byte)CharacterOpcode.EnterGameServerRes, message.Opcode.Command);
            CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
