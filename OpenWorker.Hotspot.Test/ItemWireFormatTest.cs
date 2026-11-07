using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Maze.Responses;

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
    public void SocketListHasOneByteCountAndTheSerialOfTheGear()
    {
        var socket = new ST_SOCKET_DATA
        {
            biEquipSerial = new SerialValue(0x0102030405060708),
            dwSocketID = 9,
            bySocketPos = 2,
            stExtendOption = Enumerable.Range(0, 5).Select(_ => new ItemOption(1, 2)).ToList()
        };

        var bytes = ToBytes(writer => writer.Write((IReadOnlyCollection<ST_SOCKET_DATA>)[socket]));

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

        var bytes = ToBytes(writer => writer.Write((IReadOnlyCollection<ST_ITEM_BROACH>)[broach]));

        Assert.HasCount(1 + 8 + 15 * 4, bytes);
        Assert.AreEqual(1, bytes[0]);
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
    public void MazeRewardWindowHasTheSizeTheClientReads()
    {
        // A byte, two numbers, a short, nine numbers, three pairs of a short and a number or byte.
        Assert.HasCount(1 + 4 + 4 + 2 + 9 * 4 + 2 + 4 + 2 + 4 + 2 + 1, ToBytes(new MazeRewardInfoResponse { Rank = 5 }.Write));
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
}
