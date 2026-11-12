using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The sizes of what the client reads for sockets, broaches and the answers of the inventory.
/// </summary>
[TestClass]
public sealed class ItemWireFormatTest
{
    private static byte[] ToBytes(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    [TestMethod]
    public void TheAnswerToTheUseOfAnItemIsTheItemAndWhatItGaveAsTheClientReadsIt()
    {
        var answer = new ItemUseResponse
        {
            Item = 16909060,
            Gain = new ItemUseGainValue
            {
                Experience = 1, Gold = 2, BattlePoint = 3, Ether = 4, Title = 5, SecondTitle = 6, Helper = 7,
                Items = [new ItemUseGainItem { Item = 168496141, Count = 258 }]
            }
        };

        var bytes = ToBytes(writer => answer.Write(writer));

        // The item, seven numbers of four bytes, then the count of items as one byte and each item as four bytes and two.
        Assert.HasCount(4 + 7 * 4 + 1 + 4 + 2, bytes);
        CollectionAssert.AreEqual(new byte[] { 4, 3, 2, 1 }, bytes[..4]);
        CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0 }, bytes[4..12], "Experience, then gold");
        CollectionAssert.AreEqual(new byte[] { 7, 0, 0, 0, 1, 0x0D, 0x0C, 0x0B, 0x0A, 0x02, 0x01 }, bytes[28..]);
    }

    [TestMethod]
    public void TheMotionOfAnItemIsTheActorTheItemAndTheSlotAndTheLimitsOfADayAreTheirCountedList()
    {
        var motion = ToBytes(writer => new ItemUseBtResponse { Actor = 1, Item = 2, Slot = 3 }.Write(writer));

        CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 3, 0 }, motion);

        var list = ToBytes(writer => new ItemUseInfoListResponse
        {
            UpdatedAt = 5,
            List = [new ItemUseInfoValue { Type = 12, Count = 2, UsedAt = 9 }]
        }.Write(writer));

        // When it was updated, the count of entries as one byte, then the kind, how often and when each was used.
        Assert.HasCount(8 + 1 + 4 + 1 + 8, list);
        Assert.AreEqual(5, list[0]);
        Assert.AreEqual(1, list[8]);
        Assert.AreEqual(12, list[9]);
        Assert.AreEqual(2, list[13]);
        Assert.AreEqual(9, list[14]);
    }

    [TestMethod]
    public void SocketListHasOneByteCountAndTheSerialOfTheGear()
    {
        var socket = new ST_SOCKET_DATA
        {
            biEquipSerial = new SerialValue(0x0102030405060708),
            dwSocketID = 9,
            bySocketPos = 2,
            stExtendOption = Enumerable.Range(0, 5).Select(_ => new ItemOption(1, 2)).ToList()
        };

        var bytes = ToBytes(new ItemSocketLoadResponse { Sockets = [socket] }.Write);

        Assert.HasCount(1 + 8 + 4 + 1 + 5 * 6, bytes);
        Assert.AreEqual(1, bytes[0]);
        Assert.AreEqual(0x08, bytes[1]);
    }

    [TestMethod]
    public void BroachListHasOneByteCountAndFifteenItems()
    {
        var broach = new ST_ITEM_BROACH
        {
            biSerial = new SerialValue(5),
            dwItemID = Enumerable.Range(1, ST_ITEM_BROACH.ItemCount).ToArray()
        };

        var bytes = ToBytes(new ItemBroachLoadResponse { Broaches = [broach] }.Write);

        Assert.HasCount(1 + 8 + 15 * 4, bytes);
        Assert.AreEqual(1, bytes[0]);
    }

    [TestMethod]
    public void BroachEquipAnswerIsOneBroachWithoutCount()
    {
        var broach = new ST_ITEM_BROACH
        {
            biSerial = new SerialValue(0x0102030405060708),
            dwItemID = Enumerable.Repeat(-1, ST_ITEM_BROACH.ItemCount).ToArray()
        };

        var bytes = ToBytes(new ItemSetupBroachEquipResponse { Broach = broach }.Write);

        Assert.HasCount(8 + 15 * 4, bytes);
        Assert.AreEqual(0x08, bytes[0]);
        Assert.AreEqual(-1, BitConverter.ToInt32(bytes, 8));
    }

    [TestMethod]
    public void LineUpEntryHasTheSizeTheClientReads()
    {
        var response = new ItemLineUpResponse { Entries = [new ItemLineUpEntry(StorageGroup.Common, 3, 77, 5)] };

        var bytes = ToBytes(response.Write);

        Assert.HasCount(2 + 1 + 2 + 8 + 2, bytes);
        Assert.AreEqual(1, bytes[0]);
    }

    [TestMethod]
    public void QuickSlotsHaveTheNumbersTheClientReads()
    {
        Assert.HasCount(8 * 4, ToBytes(new ItemLoadQuickslotResponse { Cards = [1], Items = [2] }.Write));
        Assert.HasCount(5 * 4, ToBytes(new ItemUpdateQuickslotCardResponse { Cards = [1, 2, 3, 4, 5] }.Write));
        Assert.HasCount(2 * 4, ToBytes(new ItemUpdateQuickslotItemResponse { Items = [1, 2] }.Write));
    }

    [TestMethod]
    public void BankAnswerListsItemsLikeTheBags()
    {
        Assert.HasCount(4, ToBytes(new ItemBankInfoResponse { List = [] }.Write));
        Assert.HasCount(4, ToBytes(new ItemUpdateResponse { List = [] }.Write));
    }

    [TestMethod]
    public void MazeRewardWindowPutsEachFieldWhereTheClientReadsIt()
    {
        var bytes = ToBytes(new MazeRewardInfoResponse { Rank = 5, ClearTime = 3661, TotalCombo = 7, PlayGold = 9, CashItem = 1234, CashItemCount = 2 }.Write);

        // A byte, two numbers, the time as a short, eight numbers, three pairs of an item and its count, and a byte.
        Assert.HasCount(1 + 4 + 4 + 2 + 8 * 4 + 3 * (4 + 2) + 1, bytes);
        Assert.AreEqual(5, bytes[0]);

        // The client splits the short into hours, minutes and seconds, and offers the keys when the item and the count of
        // the second pair are both above zero: the reward was in the wrong places before.
        Assert.AreEqual(3661, BitConverter.ToUInt16(bytes, 9));
        Assert.AreEqual(7, BitConverter.ToInt32(bytes, 11));
        Assert.AreEqual(9, BitConverter.ToInt32(bytes, 19));
        Assert.AreEqual(1234, BitConverter.ToInt32(bytes, 49));
        Assert.AreEqual(2, BitConverter.ToInt16(bytes, 53));
    }

    [TestMethod]
    public void MazeRewardGroupListsEachMember()
    {
        var bytes = ToBytes(new MazeRewardPartyInfoResponse { Members = [new MazeRewardPartyMember(1, 5, 7, "Haru", 100)] }.Write);

        Assert.AreEqual(1, bytes[0]);
        Assert.AreEqual(1, bytes[1]);
        Assert.AreEqual(5, bytes[2]);
        Assert.AreEqual(7, BitConverter.ToInt32(bytes, 3));
        Assert.AreEqual(100, BitConverter.ToInt32(bytes, bytes.Length - 4));
    }

    [TestMethod]
    public void EvolutionRequestReadsTheItemInTheLayoutTheServerWritesIt()
    {
        var item = new ItemValue
        {
            Item = 113000030,
            Serial = new SerialValue(0x0102030405060708),
            Count = 1,
            BindState = ItemBindState.Bound,
            OptionList = Enumerable.Range(1, 5).Select(e => new ItemOption((short)e, e * 10)).ToList(),
            Upgrade = 9,
            Endurance = 80,
            SocketActiveCount = 2,
            DueTo = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000),
            UpgradeCount = 3,
            UpgradeLimit = 4,
            Flag = 2,
            Exp = 1234,
            BroachState = "000000000000000",
            RestoreCount = 5,
            SealCount = 6
        };

        var bytes = ToBytes(writer =>
        {
            writer.Write(7);
            writer.Write((byte)StorageGroup.Common);
            writer.Write((short)12);
            writer.Write(item);
        });

        var reader = new PacketReader(bytes);
        var request = ItemSetupEvolutionRequest.Read(ref reader);

        Assert.AreEqual(0, reader.Remaining);
        Assert.AreEqual(7, request.Npc);
        Assert.AreEqual(12, request.Target.Slot);
        Assert.AreEqual(item.Serial.Number, request.Item.Serial.Number);
        Assert.AreEqual(50, request.Item.OptionList[4].Option);
        Assert.AreEqual(item.DueTo, request.Item.DueTo);
        Assert.AreEqual(item.BroachState, request.Item.BroachState);
        Assert.AreEqual(6, request.Item.SealCount);
    }
}
