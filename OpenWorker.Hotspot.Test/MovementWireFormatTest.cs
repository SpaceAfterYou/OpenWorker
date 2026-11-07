using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Hotspot.Modules.Movement.Responses;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class MovementWireFormatTest
{
    private const int Actor = 0x123456;
    private const long Map = 0x0102030405060708;

    private static BinaryReader Request(Action<BinaryWriter> write)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();
        stream.Position = 0;

        return new BinaryReader(stream);
    }

    private static byte[] Written(IResponseHotspotMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    private static void WriteHead(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(1f);
        writer.Write(2f);
        writer.Write(3f);
        writer.Write(4f);
    }

    [TestMethod]
    public void MoveRequestReadsWhatTheClientSends()
    {
        using var reader = Request(w =>
        {
            w.Write(Actor);
            w.Write(Map);
            w.Write(1f);
            w.Write(2f);
            w.Write(3f);
            w.Write(4f);
            w.Write(5f);
            w.Write(6f);
            w.Write((byte)7);
            w.Write(8f);
            w.Write(9f);
            w.Write((byte)10);
        });

        var request = new MovementMoveRequest(reader);

        Assert.AreEqual(Actor, request.dwActorID.Identifier);
        Assert.AreEqual(6f, request.fTargetPosY);
        Assert.AreEqual((byte)10, request.byChangeMotion);
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void PositionRequestReadsWhatTheClientSends()
    {
        using var reader = Request(w =>
        {
            w.Write(Actor);
            w.Write(Map);
            w.Write(1f);
            w.Write(2f);
            w.Write(3f);
            w.Write(4f);
            w.Write(5f);
        });

        var request = new MovementPositionRequest(reader);

        Assert.AreEqual(3f, request.fPosZ);
        Assert.AreEqual(5f, request.fPitch);
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void IgnoreMotionDeltaRequestReadsWhatTheClientSends()
    {
        using var reader = Request(w =>
        {
            WriteHead(w);
            w.Write(5f);
        });

        var request = new MovementIgnoreMotionDeltaRequest(reader);

        Assert.AreEqual(4f, request.fYaw);
        Assert.AreEqual(5f, request.fPitch);
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void LoopMotionStartRequestReadsWhatTheClientSends()
    {
        using var reader = Request(w =>
        {
            WriteHead(w);
            w.Write(5);
            w.Write(6);
            w.Write(7);
            w.Write(8);
        });

        var request = new MovementLoopMotionStartRequest(reader);

        Assert.AreEqual(5, request.dwStartAniID);
        Assert.AreEqual(8, request.dwIdleAniID);
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void AttacedEndRequestReadsWhatTheClientSends()
    {
        using var reader = Request(w =>
        {
            w.Write(Actor);
            w.Write(1f);
            w.Write(2f);
            w.Write(3f);
        });

        var request = new MovementAttacedEndRequest(reader);

        Assert.AreEqual(3f, request.fPosZ);
        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void BattleResponseHasTheLayoutTheClientReads()
    {
        var bytes = Written(new MovementBattleBtResponse
        {
            Actor = Actor,
            Pos = new Vector3(1, 2, 3),
            Yaw = 4,
            BattlePose = 5,
            PlayMotion = 6
        });

        Assert.HasCount(4 + 4 * 4 + 1 + 1, bytes);
        Assert.AreEqual(4f, BitConverter.ToSingle(bytes, 16));
        Assert.AreEqual((byte)5, bytes[20]);
        Assert.AreEqual((byte)6, bytes[21]);
    }

    [TestMethod]
    public void MotionResponseHasTheLayoutTheClientReads()
    {
        var bytes = Written(new MovementMotionBtResponse { Actor = Actor, MotionClass = 7, SubClass = 8 });

        Assert.HasCount(4 + 2 + 2, bytes);
        Assert.AreEqual((short)8, BitConverter.ToInt16(bytes, 6));
    }

    [TestMethod]
    public void IgnoreMotionDeltaResponseEndsWithTheForcedFlag()
    {
        var bytes = Written(new MovementIgnoreMotionDeltaResponse
        {
            Actor = Actor,
            Pos = new Vector3(1, 2, 3),
            Yaw = 4,
            Pitch = 5,
            Forced = 1
        });

        Assert.HasCount(4 + 5 * 4 + 1, bytes);
        Assert.AreEqual((byte)1, bytes[^1]);
    }

    [TestMethod]
    public void LoopMotionStartResponseHasTheLayoutTheClientReads()
    {
        var bytes = Written(new MovementLoopMotionStartBtResponse
        {
            Actor = Actor,
            Pos = new Vector3(1, 2, 3),
            Yaw = 4,
            StartAnimation = 5,
            LoopAnimation = 6,
            EndAnimation = 7,
            IdleAnimation = 8
        });

        Assert.HasCount(4 + 4 * 4 + 4 * 4, bytes);
        Assert.AreEqual(8, BitConverter.ToInt32(bytes, 32));
    }

    [TestMethod]
    public void ResponsesThatOnlyNameTheActorAreFourBytes()
    {
        Assert.HasCount(4, Written(new MovementLoopMotionEndBtResponse { Actor = Actor }));
        Assert.HasCount(4, Written(new MovementAttacedEndBtResponse { Actor = Actor }));
    }

    [TestMethod]
    public void MoveResponseHasTheLayoutTheClientReads()
    {
        var bytes = Written(new MovementMoveBtResponse
        {
            Actor = Actor,
            Map = default(MapValue),
            Pos = new Vector3(1, 2, 3),
            Yaw = 4,
            TargetPosX = 5,
            TargetPosY = 6,
            RunBit = 7,
            Pitch = 8,
            MoveSpeed = 9,
            ChangeMotion = 10
        });

        Assert.HasCount(4 + 8 + 6 * 4 + 1 + 2 * 4 + 1, bytes);
        Assert.AreEqual((byte)7, bytes[36]);
        Assert.AreEqual((byte)10, bytes[^1]);
    }
}
