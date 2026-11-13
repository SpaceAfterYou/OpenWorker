using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Responses;
using OpenWorker.Hotspot.Modules.Boosters.Types;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.Hotspot.Modules.DailyMissions.Requests;
using OpenWorker.Hotspot.Modules.DailyMissions.Responses;
using OpenWorker.Hotspot.Modules.DailyMissions.Types;
using OpenWorker.Hotspot.Modules.Events.Enums;
using OpenWorker.Hotspot.Modules.Events.Requests;
using OpenWorker.Hotspot.Modules.Events.Responses;
using OpenWorker.Hotspot.Modules.Events.Types;
using OpenWorker.Hotspot.Modules.Rewards.Types;
using OpenWorker.Hotspot.Modules.Vaccum.Requests;
using OpenWorker.Hotspot.Modules.Vaccum.Responses;
using OpenWorker.Hotspot.Modules.Vaccum.Types;
using OpenWorker.Hotspot.Modules.SoulMetry;
using OpenWorker.Hotspot.Modules.SoulMetry.Requests;
using OpenWorker.Hotspot.Modules.SoulMetry.Responses;
using OpenWorker.Hotspot.Modules.SoulMetry.Types;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The layouts of the messages of soul metry, daily missions, helpers, boosters, events and vaccum cubes
/// as the client reads and writes them.
/// </summary>
[TestClass]
public sealed class MiscWireFormatTest
{
    private static byte[] Written(IResponseHotspotMessage message) => WireBytes.Of(message);

#region SoulMetry

    [TestMethod]
    public void SoulMetryListIsCountAndIdWithBits()
    {
        var bytes = Written(new SoulMetryListResponse
        {
            Values = [new SoulMetryValue(32, 1016), new SoulMetryValue(33, 7)]
        });

        Assert.HasCount(2 + 2 * 6, bytes);
        Assert.AreEqual((short)2, BitConverter.ToInt16(bytes));
        Assert.AreEqual(32, BitConverter.ToInt32(bytes, 2));
        Assert.AreEqual((short)1016, BitConverter.ToInt16(bytes, 6));
        Assert.AreEqual(33, BitConverter.ToInt32(bytes, 8));
    }

    [TestMethod]
    public void SoulMetryAddAndUpdateAreIdAndBits()
    {
        var value = new SoulMetryValue(35, 1017);

        var added = Written(new SoulMetryAddResponse { Data = value });
        var updated = Written(new SoulMetryUpdateResponse { Data = value });

        Assert.HasCount(6, added);
        CollectionAssert.AreEqual(added, updated);
        Assert.AreEqual(35, BitConverter.ToInt32(added));
        Assert.AreEqual((short)1017, BitConverter.ToInt16(added, 4));
    }

    [TestMethod]
    public void SoulMetryCompleteListIsTheBitmapOfFixedSize()
    {
        var complete = new byte[SoulMetryModuleDefines.CompleteListByteLength];
        complete[0] = 0b10;

        var bytes = Written(new SoulMetryCompleteListResponse { Complete = complete });

        Assert.HasCount(128, bytes);
        Assert.AreEqual((byte)0b10, bytes[0]);
    }

    [TestMethod]
    public void SoulMetryCompleteListOfAnotherSizeIsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => Written(new SoulMetryCompleteListResponse { Complete = new byte[256] }));
    }

    [TestMethod]
    public void SoulMetryCompleteIsIdAndTheItemsWithByteCount()
    {
        var bytes = Written(new SoulMetryCompleteResponse
        {
            Identifier = 35,
            Rewards = [new RewardItemValue(711146322, 1), new RewardItemValue(5, 2)]
        });

        Assert.HasCount(4 + 1 + 2 * 6, bytes);
        Assert.AreEqual(35, BitConverter.ToInt32(bytes));
        Assert.AreEqual((byte)2, bytes[4]);
        Assert.AreEqual(711146322, BitConverter.ToInt32(bytes, 5));
        Assert.AreEqual((short)1, BitConverter.ToInt16(bytes, 9));
        Assert.AreEqual(5, BitConverter.ToInt32(bytes, 11));
    }

    [TestMethod]
    public void SoulMetryCompleteWithoutItemsHasZeroCount()
    {
        var bytes = Written(new SoulMetryCompleteResponse { Identifier = 35, Rewards = [] });

        CollectionAssert.AreEqual(new byte[] { 35, 0, 0, 0, 0 }, bytes);
    }

    [TestMethod]
    public void SoulMetryResetIsEmpty()
    {
        Assert.HasCount(0, Written(new SoulMetryResetResponse()));
    }

    [TestMethod]
    public void SoulMetryCompleteRequestIsTheId()
    {
        using var stream = new MemoryStream(BitConverter.GetBytes(41));
        using var reader = new BinaryReader(stream);

        Assert.AreEqual(41, new SoulCompleteRequest(reader).Identifier);
        Assert.AreEqual(4, stream.Position);
    }

#endregion SoulMetry

#region DailyMission

    private static DailyMissionValue Mission(int id) => new()
    {
        Mission = id,
        Type = DailyMissionType.Guerrilla,
        State = DailyMissionState.Accept,
        Helper = DailyMissionHelperState.Add,
        Value = 7,
        Accept = DateTimeOffset.FromUnixTimeSeconds(1_000),
        Start = DateTimeOffset.FromUnixTimeSeconds(2_000),
        End = DateTimeOffset.FromUnixTimeSeconds(3_000),
        Remain = 11,
        Duration = 22
    };

    [TestMethod]
    public void DailyMissionIs41BytesInTheOrderOfTheClient()
    {
        var bytes = Written(new DailyMissionUpdateResponse { List = [Mission(2100001)] });

        Assert.HasCount(2 + 41, bytes);
        Assert.AreEqual((short)1, BitConverter.ToInt16(bytes));
        Assert.AreEqual(2100001, BitConverter.ToInt32(bytes, 2));
        Assert.AreEqual((byte)2, bytes[6]);
        Assert.AreEqual((byte)1, bytes[7]);
        Assert.AreEqual((byte)1, bytes[8]);
        Assert.AreEqual((short)7, BitConverter.ToInt16(bytes, 9));
        Assert.AreEqual(1_000L, BitConverter.ToInt64(bytes, 11));
        Assert.AreEqual(2_000L, BitConverter.ToInt64(bytes, 19));
        Assert.AreEqual(3_000L, BitConverter.ToInt64(bytes, 27));
        Assert.AreEqual(11, BitConverter.ToInt32(bytes, 35));
        Assert.AreEqual(22, BitConverter.ToInt32(bytes, 39));
    }

    [TestMethod]
    public void DailyMissionUpdateIsAFlatListWithShortCount()
    {
        var bytes = Written(new DailyMissionUpdateResponse { List = [Mission(1), Mission(2), Mission(3)] });

        Assert.HasCount(2 + 3 * 41, bytes);
        Assert.AreEqual((short)3, BitConverter.ToInt16(bytes));
    }

    [TestMethod]
    public void DailyMissionListIsFlagThenGroupsOfADistrict()
    {
        var bytes = Written(new DailyMissionListResponse
        {
            TodayInit = true,
            Groups =
            [
                new DailyMissionGroupValue { District = 10003, Missions = [Mission(1), Mission(2)] },
                new DailyMissionGroupValue { District = 10021, Missions = [Mission(3)] }
            ]
        });

        Assert.HasCount(1 + 2 + (4 + 2 + 2 * 41) + (4 + 2 + 41), bytes);
        Assert.AreEqual((byte)1, bytes[0]);
        Assert.AreEqual((short)2, BitConverter.ToInt16(bytes, 1));
        Assert.AreEqual(10003, BitConverter.ToInt32(bytes, 3));
        Assert.AreEqual((short)2, BitConverter.ToInt16(bytes, 7));
        Assert.AreEqual(1, BitConverter.ToInt32(bytes, 9));
        Assert.AreEqual(10021, BitConverter.ToInt32(bytes, 9 + 2 * 41));
    }

    [TestMethod]
    public void EmptyDailyMissionListHasNoGroups()
    {
        CollectionAssert.AreEqual(new byte[] { 0, 0, 0 }, Written(new DailyMissionListResponse { Groups = [] }));
    }

    [TestMethod]
    public void DailyMissionAcceptAndHelperAreMissionAndByte()
    {
        var accepted = Written(new DailyMissionAcceptResponse { Mission = 1100001, Helper = DailyMissionHelperState.Add });
        var helper = Written(new DailyMissionHelperResponse { Mission = 1100001, Helper = DailyMissionHelperState.Del });

        CollectionAssert.AreEqual(BitConverter.GetBytes(1100001).Append((byte)1).ToArray(), accepted);
        CollectionAssert.AreEqual(BitConverter.GetBytes(1100001).Append((byte)0).ToArray(), helper);
    }

    [TestMethod]
    public void DailyMissionRequestsAreReadLikeTheClientWritesThem()
    {
        using var accept = new BinaryReader(new MemoryStream(BitConverter.GetBytes(1100002)));
        using var helper = new BinaryReader(new MemoryStream(BitConverter.GetBytes(1100003).Append((byte)1).ToArray()));

        Assert.AreEqual(1100002, new DailyMissionAcceptRequest(accept).Mission);

        var request = new DailyMissionHelperRequest(helper);

        Assert.AreEqual(1100003, request.Mission);
        Assert.AreEqual(DailyMissionHelperState.Add, request.Helper);
        Assert.AreEqual(5, helper.BaseStream.Position);
    }

#endregion DailyMission

#region Events

    private static AttendanceInfo Attendance(int count, int table = 1001)
    {
        return new AttendanceInfo
        {
            CurrentDay = (byte)count,
            State = Enumerable.Range(0, AttendanceInfo.Days).Select(e => e < count ? AttendanceState.Taken : AttendanceState.Available).ToArray(),
            Table = table
        };
    }

    [TestMethod]
    public void AttendanceIsDayCountThenACharacterForEveryDayThenTheTable()
    {
        var bytes = Written(new EventAttendanceRewardResponse { Info = Attendance(2) });

        Assert.HasCount(45, bytes);
        Assert.AreEqual((byte)2, bytes[0]);
        Assert.AreEqual((byte)'1', bytes[1]);
        Assert.AreEqual((byte)'1', bytes[2]);
        Assert.AreEqual((byte)'0', bytes[3]);
        Assert.AreEqual((byte)'0', bytes[40]);
        Assert.AreEqual(1001, BitConverter.ToInt32(bytes, 41));
    }

    [TestMethod]
    public void AttendanceNoticeHasTheSameLayout()
    {
        CollectionAssert.AreEqual(
            Written(new EventAttendanceRewardResponse { Info = Attendance(5) }),
            Written(new EventAttendanceNoticeResponse { Info = Attendance(5) }));
    }

    [TestMethod]
    public void AttendanceLoadIsTheAttendanceAndThePlayTime()
    {
        var bytes = Written(new EventAttendanceLoadResponse
        {
            Info = Attendance(1),
            Time = new AttendancePlayTime { Step = 2, Seconds = 77 }
        });

        Assert.HasCount(45 + 5, bytes);
        Assert.AreEqual((byte)2, bytes[45]);
        Assert.AreEqual(77, BitConverter.ToInt32(bytes, 46));
    }

    [TestMethod]
    public void AttendanceOfAnotherNumberOfDaysIsRefused()
    {
        var info = new AttendanceInfo { CurrentDay = 1, State = new AttendanceState[28], Table = 1 };

        Assert.Throws<InvalidOperationException>(() => Written(new EventAttendanceRewardResponse { Info = info }));
    }

    [TestMethod]
    public void PlayTimeRewardIsStepAndSeconds()
    {
        var bytes = Written(new EventAttendancePlayTimeRewardResponse { Time = new AttendancePlayTime { Step = 1, Seconds = 0 } });

        CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0, 0 }, bytes);
    }

    [TestMethod]
    public void PlayTimeInitHasNoContent()
    {
        Assert.HasCount(0, Written(new EventAttendancePlayTimeInitResponse()));
    }

    [TestMethod]
    public void LuaValuesAreCountThenLengthAndCharacters()
    {
        var bytes = Written(new EventLuaValuesResponse { Scripts = ["ab", "cde"] });

        CollectionAssert.AreEqual(new byte[] { 2, 2, 0, (byte)'a', (byte)'b', 3, 0, (byte)'c', (byte)'d', (byte)'e' }, bytes);
    }

    [TestMethod]
    public void LuaValueTooLongForTheClientIsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => Written(new EventLuaValuesResponse { Scripts = [new string('x', 30)] }));
        Assert.Throws<InvalidOperationException>(() => Written(new EventLuaValuesResponse { Scripts = [""] }));
        Assert.HasCount(1 + 2 + 29, Written(new EventLuaValuesResponse { Scripts = [new string('x', 29)] }));
    }

    [TestMethod]
    public void AttendanceRewardRequestIsTheDayAndPlayTimeRequestIsEmpty()
    {
        using var day = new BinaryReader(new MemoryStream([7]));

        Assert.AreEqual((byte)7, new EventAttendanceRewardRequest(day).DayIndex);
        Assert.AreEqual(1, day.BaseStream.Position);
    }

#endregion Events

#region Booster

    [TestMethod]
    public void BoosterListIsAreaCountAndIdWithEightBytesOfSeconds()
    {
        var bytes = Written(new BoosterLoadResponse
        {
            Area = BoosterConsumeArea.Maze,
            Entries = [new BoosterLoadEntry(2154, 3600), new BoosterLoadEntry(101, BoosterLoadEntry.Endless)]
        });

        Assert.HasCount(1 + 2 + 2 * 10, bytes);
        Assert.AreEqual((byte)2, bytes[0]);
        Assert.AreEqual((short)2, BitConverter.ToInt16(bytes, 1));
        Assert.AreEqual((short)2154, BitConverter.ToInt16(bytes, 3));
        Assert.AreEqual(3600L, BitConverter.ToInt64(bytes, 5));
        Assert.AreEqual((short)101, BitConverter.ToInt16(bytes, 13));
        Assert.AreEqual(-1L, BitConverter.ToInt64(bytes, 15));
    }

    [TestMethod]
    public void EmptyBoosterListStillClearsTheClient()
    {
        CollectionAssert.AreEqual(new byte[] { 1, 0, 0 }, Written(new BoosterLoadResponse { Area = BoosterConsumeArea.District, Entries = [] }));
    }

    [TestMethod]
    public void BoosterAddIsAreaThenIdThenSeconds()
    {
        var bytes = Written(new BoosterAddResponse { Area = BoosterConsumeArea.District, Identifier = 2155, RemainSeconds = 7200 });

        Assert.HasCount(1 + 2 + 8, bytes);
        Assert.AreEqual((byte)1, bytes[0]);
        Assert.AreEqual((short)2155, BitConverter.ToInt16(bytes, 1));
        Assert.AreEqual(7200L, BitConverter.ToInt64(bytes, 3));
    }

    [TestMethod]
    public void BoosterRemoveIsTheIdAndClearIsEmpty()
    {
        CollectionAssert.AreEqual(BitConverter.GetBytes((short)2155), Written(new BoosterRemoveResponse { Id = 2155 }));
        Assert.HasCount(0, Written(new BoosterClearResponse()));
    }

#endregion Booster

#region Vaccum

    [TestMethod]
    public void VaccumClickAnswersAreActorCubeAndResult()
    {
        var start = Written(new VaccumClickStartResponse { Actor = 1, Cube = 2, Result = 3 });
        var cancel = Written(new VaccumClickCancelResponse { Actor = 1, Cube = 2, Result = 55800 });

        Assert.HasCount(12, start);
        Assert.AreEqual(2, BitConverter.ToInt32(start, 4));
        Assert.AreEqual(3, BitConverter.ToInt32(start, 8));
        Assert.HasCount(12, cancel);
        Assert.AreEqual(55800, BitConverter.ToInt32(cancel, 8));
    }

    [TestMethod]
    public void VaccumCompleteIsActorCubeRemainItemsAndResult()
    {
        var bytes = Written(new VaccumClickCompleteResponse
        {
            Actor = 1,
            Cube = 2,
            Remain = 3,
            Items = [new RewardItemValue(10, 4)],
            Result = 5
        });

        Assert.HasCount(4 + 4 + 2 + 1 + 6 + 4, bytes);
        Assert.AreEqual((short)3, BitConverter.ToInt16(bytes, 8));
        Assert.AreEqual((byte)1, bytes[10]);
        Assert.AreEqual(5, BitConverter.ToInt32(bytes, 17));
    }

    [TestMethod]
    public void VaccumCubesAreInAndOutAndAListWithByteCount()
    {
        Assert.HasCount(9, Written(new VaccumInResponse { Cube = 1, Value = 2, State = 3 }));
        Assert.HasCount(5, Written(new VaccumOutResponse { Cube = 1, Flag = 0 }));

        var infos = Written(new VaccumInfosResponse { Cubes = [new VaccumCubeValue(1, 2, 3), new VaccumCubeValue(4, 5, 6)] });

        Assert.HasCount(1 + 2 * 9, infos);
        Assert.AreEqual((byte)2, infos[0]);
        Assert.AreEqual(4, BitConverter.ToInt32(infos, 10));
    }

    [TestMethod]
    public void VaccumRequestsAreTheCube()
    {
        using var start = new BinaryReader(new MemoryStream(BitConverter.GetBytes(30001)));
        using var cancel = new BinaryReader(new MemoryStream(BitConverter.GetBytes(30002)));

        Assert.AreEqual(30001, new VaccumClickStartRequest(start).Cube);
        Assert.AreEqual(30002, new VaccumClickCancelRequest(cancel).Cube);
    }

#endregion Vaccum
}
