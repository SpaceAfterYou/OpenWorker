using System.Numerics;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Hotspot.Modules.World.Types;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class MazeWireFormatTest
{
    private static byte[] Written(IResponseHotspotMessage message) => WireBytes.Of(message);

    [TestMethod]
    public void MonsterSuperArmorIsActorAndTwoFloats()
    {
        var data = Written(new MonsterSuperArmorGageResponse { Current = 1.5f, Max = 2.5f });

        Assert.HasCount(12, data);
        Assert.AreEqual(1.5f, BitConverter.ToSingle(data, 4));
        Assert.AreEqual(2.5f, BitConverter.ToSingle(data, 8));
    }

    [TestMethod]
    public void MonsterPartsAreActorAndFourNumbers()
    {
        var data = Written(new MonsterPartsHpResponse { FirstCurrent = 1, FirstMax = 2, SecondCurrent = 3, SecondMax = 4 });

        Assert.HasCount(20, data);
        Assert.AreEqual(1, BitConverter.ToInt32(data, 4));
        Assert.AreEqual(4, BitConverter.ToInt32(data, 16));
    }

    [TestMethod]
    public void MonsterStatListPutsTheValueBeforeTheStat()
    {
        var data = Written(new MonsterUpdateStatListResponse
        {
            Stats = [new StatKeyValuePair { byIndex = STAT_TYPE.STAT_HP_MAX, statValue = 7f }]
        });

        Assert.HasCount(4 + 1 + 4 + 2, data);
        Assert.AreEqual(1, data[4]);
        Assert.AreEqual(7f, BitConverter.ToSingle(data, 5));
        Assert.AreEqual((short)STAT_TYPE.STAT_HP_MAX, BitConverter.ToInt16(data, 9));
    }

    [TestMethod]
    public void MonsterDieIsActorMotionAndType()
    {
        var bytes = Written(new MonsterDieResponse { Actor = 7, Motion = 8, DieType = 9 });

        Assert.HasCount(4 + 2 + 1, bytes);
        Assert.AreEqual((short)8, BitConverter.ToInt16(bytes, 4));
        Assert.AreEqual((byte)9, bytes[6]);
    }

    [TestMethod]
    public void PortalEnableIsBoxAndTwoFlags()
    {
        var bytes = Written(new WorldWarpEnableResponse { Box = 30008, Open = true });

        Assert.HasCount(4 + 1 + 1, bytes);
        Assert.AreEqual(30008, BitConverter.ToInt32(bytes));
        Assert.AreEqual((byte)1, bytes[4]);
        Assert.AreEqual((byte)0, bytes[5]);
    }

    [TestMethod]
    public void GateEnableIsBoxFlagMazeFlagAndTheSectorBehind()
    {
        var bytes = Written(new WorldGateEnableResponse { Gate = 20001, Open = true, IsMazeComplete = true, NextSector = 10003 });

        Assert.HasCount(4 + 1 + 1 + 4, bytes);
        Assert.AreEqual(20001, BitConverter.ToInt32(bytes));
        Assert.AreEqual((byte)1, bytes[4]);
        Assert.AreEqual((byte)1, bytes[5]);
        Assert.AreEqual(10003, BitConverter.ToInt32(bytes, 6));
    }

    [TestMethod]
    public void SectorInfosEndWithTheCurrentSector()
    {
        var bytes = Written(new MazeSectorInfosResponse { Cleared = [1, 2], Current = 3 });

        Assert.HasCount(4 + 2 * 4 + 4, bytes);
        Assert.AreEqual(2, BitConverter.ToInt32(bytes));
        Assert.AreEqual(3, BitConverter.ToInt32(bytes, 12));
    }

    [TestMethod]
    public void GateInfosStartWithTheReEnterFlag()
    {
        var bytes = Written(new MazeGateInfosResponse { ReEnter = true, Open = [7] });

        Assert.HasCount(1 + 4 + 4, bytes);
        Assert.AreEqual((byte)1, bytes[0]);
        Assert.AreEqual(1, BitConverter.ToInt32(bytes, 1));
    }

    [TestMethod]
    public void PortalInfosHaveBothCountsFirst()
    {
        var bytes = Written(new MazePortalInfosResponse { Open = [1, 2], Closed = [3] });

        Assert.HasCount(4 + 4 + 3 * 4, bytes);
        Assert.AreEqual(2, BitConverter.ToInt32(bytes));
        Assert.AreEqual(1, BitConverter.ToInt32(bytes, 4));
        Assert.AreEqual(3, BitConverter.ToInt32(bytes, 16));
    }

    [TestMethod]
    public void InteractionInfosHaveAByteCount()
    {
        var bytes = Written(new MazeInteractionInfosResponse
        {
            Interactions = [new MazeInteractionValue(true, false, 701, 1)]
        });

        Assert.HasCount(1 + 1 + 1 + 4 + 4, bytes);
        Assert.AreEqual(701, BitConverter.ToInt32(bytes, 3));
    }

    [TestMethod]
    public void InteractionRunStartsWithTheResult()
    {
        var bytes = Written(new MazeInteractionRunResponse
        {
            Result = 0,
            Interaction = new MazeInteractionValue(true, true, 701, 1)
        });

        Assert.HasCount(1 + 1 + 1 + 4 + 4, bytes);
        Assert.AreEqual(701, BitConverter.ToInt32(bytes, 3));
    }

    [TestMethod]
    public void ClientSyncIsTwoNumbers()
    {
        var bytes = Written(new MazeLuaClientSyncResponse { Type = 3, Value = 9 });

        Assert.HasCount(8, bytes);
        Assert.AreEqual(3, BitConverter.ToInt32(bytes));
        Assert.AreEqual(9, BitConverter.ToInt32(bytes, 4));
    }

    [TestMethod]
    public void TimerSkipsFourBytesBeforeTheTextAndTheTimes()
    {
        var bytes = Written(new MazeTimerResponse { Value = 5, Time = 60f, Next = 10f });

        Assert.HasCount(16, bytes);
        Assert.AreEqual(5, BitConverter.ToInt32(bytes, 4));
        Assert.AreEqual(60f, BitConverter.ToSingle(bytes, 8));
        Assert.AreEqual(10f, BitConverter.ToSingle(bytes, 12));
    }

    [TestMethod]
    public void HiddenEventIsTheEventTheConditionTheStateAndTheCount()
    {
        var bytes = Written(new MazeHiddenEventResponse { Event = 6007, Condition = 600701, State = 2, Count = 15 });

        CollectionAssert.AreEqual(new byte[] { 0x77, 0x17, 0x7D, 0x2A, 0x09, 0x00, 0x02, 0x0F, 0x00, 0x00, 0x00 }, bytes);
    }

    [TestMethod]
    public void WarpInSectorIsAPlaceAndAYaw()
    {
        var bytes = Written(new MazeWarpInSectorResponse { Position = new Vector3(1, 2, 3), Rotation = 90f });

        Assert.HasCount(16, bytes);
        Assert.AreEqual(90f, BitConverter.ToSingle(bytes, 12), "the client turns the player by a float");
    }

    [TestMethod]
    public void PortalWarpIsThePortalThePlaceAndTheSector()
    {
        var bytes = Written(new WorldWarpMazeResponse
        {
            BoxIndex = 30001,
            Warp = new WarpValue { HasError = false, Position = new Vector3(1, 2, 3), Rotation = 45f },
            Sector = 10002
        });

        Assert.HasCount(4 + 1 + 12 + 4 + 4, bytes);
        Assert.AreEqual(30001, BitConverter.ToInt32(bytes));
        Assert.AreEqual((byte)0, bytes[4]);
        Assert.AreEqual(10002, BitConverter.ToInt32(bytes, 21));
    }

    [TestMethod]
    public void SwitchNpcAnswersAreTheTownsmanAndAByte()
    {
        Assert.HasCount(5, Written(new MazeSwitchNpcStatusResponse { Npc = 7, Status = 1 }));
        Assert.HasCount(5, Written(new MazeSwitchNpcEnableResponse { Npc = 7, Enable = true }));
    }

    [TestMethod]
    public void EscortStatusIsThePointAndAByte()
    {
        CollectionAssert.AreEqual(new byte[] { 0x91, 0x01, 0, 0, 2 }, Written(new MazeEscortStatusResponse { Point = 401, Status = EscortStatus.Arrived }));
    }

    [TestMethod]
    public void ClientRequestsOfTheMazeAreRead()
    {
        using var spawn = new BinaryReader(new MemoryStream(new byte[12 + 4 + 4 + 4]));
        using var motion = new BinaryReader(new MemoryStream([1, 5, 0, 0, 0]));
        using var party = new BinaryReader(new MemoryStream([7, 0, 0, 0, 1]));
        using var click = new BinaryReader(new MemoryStream([9, 0, 0, 0]));

        _ = new MazeMonsterClientSpawnRequest(spawn);
        Assert.AreEqual(spawn.BaseStream.Length, spawn.BaseStream.Position);

        var motionRequest = new MazeInteractionMotionRequest(motion);
        Assert.IsTrue(motionRequest.IsBroken);
        Assert.AreEqual(5, motionRequest.Box);

        var partyRequest = new MazeEnterPartyAnswerRequest(party);
        Assert.AreEqual(7, partyRequest.Maze);
        Assert.IsTrue(partyRequest.Agree);

        Assert.AreEqual(9, new MazeSwitchNpcClickRequest(click).Npc);
    }
}
