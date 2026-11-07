using System.Numerics;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the client reads when a monster moves, turns, looks at somebody or is hurt.
/// </summary>
[TestClass]
public sealed class MonsterWireFormatTest
{
    private static readonly ActorValue Monster = new(0x123456, ActorType.Monster);

    private static byte[] Written(IResponseHotspotMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    private static byte[] Expected(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    private static int Raw => new ActorComponent(Monster.Identifier, Monster.Type);

    [TestMethod]
    public void TargetChangeIsTwoActors()
    {
        var bytes = Written(new MonsterTargetChangeResponse { Actor = Monster, Target = 77 });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(Raw);
            w.Write(77);
        }), bytes);
    }

    [TestMethod]
    public void ChangeMotionHasTwoShorts()
    {
        var bytes = Written(new MonsterChangeMotionResponse { Actor = Monster, Motion = 15, SubMotion = 2 });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(Raw);
            w.Write((short)15);
            w.Write((short)2);
        }), bytes);
    }

    [TestMethod]
    public void EscapeDamageAndInvisibleHaveTheClientSize()
    {
        Assert.HasCount(4 + 1 + 4, Written(new MonsterEscapeDamageResponse { Actor = Monster, DefenseType = 2, Time = 1.5f }));
        Assert.HasCount(4 + 1 + 4 + 4 + 4, Written(new MonsterInvisibleResponse { Actor = Monster, Invisible = true, Flags = 1 }));
    }

    [TestMethod]
    public void GazeEndsWithAFlagOfFourBytes()
    {
        var bytes = Written(new MovementGazeBtResponse { Actor = Monster, Position = new Vector3(1, 2, 3), Yaw = 4, Gazing = true });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(Raw);
            w.Write(1f);
            w.Write(2f);
            w.Write(3f);
            w.Write(4f);
            w.Write(1);
        }), bytes);
    }

    [TestMethod]
    public void IdleHasTheStepAsAByteAndTheDelayAsAFloat()
    {
        var bytes = Written(new MovementIdleBtResponse
        {
            Actor = Monster,
            Position = new Vector3(1, 2, 3),
            Yaw = 4,
            Animation = 5,
            DefaultStep = 6,
            Delay = -1f
        });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(Raw);
            w.Write(1f);
            w.Write(2f);
            w.Write(3f);
            w.Write(4f);
            w.Write(5);
            w.Write((byte)6);
            w.Write(-1f);
        }), bytes);
    }

    [TestMethod]
    public void IdlesAreCountedByAByteAndTakeTwentyEightBytesEach()
    {
        var entry = new MovementIdleEntry(Monster, new Vector3(1, 2, 3), 4, 5, 6);
        var bytes = Written(new MovementIdlesBtResponse { Entries = [entry, entry] });

        Assert.HasCount(1 + 2 * 28, bytes);
        Assert.AreEqual((byte)2, bytes[0]);
    }

    [TestMethod]
    public void UpdateDirectionEndsWithAByte()
    {
        var bytes = Written(new MovementUpdateDirResponse { Actor = Monster, Yaw = 90, Pitch = 1, Direct = true });

        CollectionAssert.AreEqual(Expected(w =>
        {
            w.Write(Raw);
            w.Write(90f);
            w.Write(1f);
            w.Write(true);
        }), bytes);
    }

    [TestMethod]
    public void SmallMovementMessagesHaveTheClientSize()
    {
        Assert.HasCount(4 + 4 + 4, Written(new MovementInfoBtResponse { Actor = Monster, Type = 1, Value = 2 }));
        Assert.HasCount(4 + 4, Written(new MovementRotationResponse { Actor = Monster, Yaw = 1 }));
        Assert.HasCount(4 + 4 + 4, Written(new MovementStiffenBtResponse { Actor = Monster, Rate = 0.5f, Time = 1 }));
        Assert.HasCount(4 + 12 + 4 + 1, Written(new MovementDropResponse { Actor = Monster, Position = Vector3.One, Yaw = 1, Sync = true }));
        Assert.HasCount(4 + 12, Written(new MovementGrapResponse { Actor = Monster, Position = Vector3.One }));
    }

    [TestMethod]
    public void TraceFollowsTheOrderTheClientReads()
    {
        var bytes = Written(new MovementTraceBtResponse
        {
            Actor = Monster,
            Position = new Vector3(1, 2, 3),
            Yaw = 4,
            Pitch = 5,
            Animation = 6,
            DefenseType = 7,
            Projectiles = [Vector3.One],
            AiState = 8,
            Fuzzy = [0.5f],
            Actions = [1, 2, 3],
            MoveX = 9,
            MoveY = 10,
            WayPoint = 11,
            Aggro = [new MovementTraceAggro(12, 1.5f)]
        });

        // Actor, position, yaw, pitch, animation, defense, one projectile, state, 4 fuzzy, 7 actions, a place, a waypoint, one aggro.
        Assert.HasCount(4 + 12 + 4 + 4 + 4 + 1 + 1 + 12 + 1 + 4 * 4 + 7 * 4 + 8 + 2 + 1 + 8, bytes);

        using var reader = new BinaryReader(new MemoryStream(bytes));

        reader.ReadInt32();
        reader.ReadBytes(12);
        reader.ReadBytes(8);
        Assert.AreEqual(6, reader.ReadInt32());
        Assert.AreEqual((byte)7, reader.ReadByte());
        Assert.AreEqual((byte)1, reader.ReadByte());
        reader.ReadBytes(12);
        Assert.AreEqual((byte)8, reader.ReadByte());
        Assert.AreEqual(0.5f, reader.ReadSingle());
    }

    [TestMethod]
    public void ReviveIsWrittenWhereTheClientReadsIt()
    {
        var revive = new PersonReviveResponse();

        Assert.AreEqual((byte)CharacterOpcode.ReviveBt, revive.Opcode.Command);
        Assert.AreEqual((byte)GroupOpcode.Character, revive.Opcode.Group);
    }
}
