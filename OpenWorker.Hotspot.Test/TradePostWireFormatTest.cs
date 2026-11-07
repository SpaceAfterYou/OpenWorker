using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Post.Extensions;
using OpenWorker.Hotspot.Modules.Post.Requests;
using OpenWorker.Hotspot.Modules.Post.Responses;
using OpenWorker.Hotspot.Modules.Post.Types;
using OpenWorker.Hotspot.Modules.Trade.Requests;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class TradePostWireFormatTest
{
    private const int ItemSize = 4 + 8 + 2 + 1 + 5 * (2 + 4) + 3 + 8 + 3 + 4 + (2 + 15) + 2;

    private static ItemValue CreateItem()
    {
        return new ItemValue
        {
            OptionList = Enumerable.Range(0, 5).Select(_ => new ItemOption(0, 0)).ToList(),
            BroachState = "000000000000000"
        };
    }

    private static byte[] ToBytes(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    [TestMethod]
    public void ItemHasClientSize()
    {
        Assert.HasCount(ItemSize, ToBytes(writer => writer.Write(CreateItem())));
    }

    [TestMethod]
    [DataRow("", DisplayName = "No state is written as the default one")]
    [DataRow("000000000000000", DisplayName = "The default state")]
    [DataRow("1", DisplayName = "A short state")]
    public void BroachStateFitsWhatTheClientAccepts(string state)
    {
        var item = CreateItem();

        var bytes = ToBytes(writer => writer.Write(new ItemValue { OptionList = item.OptionList, BroachState = state }));

        // The client takes between one and fifteen characters here and has no room for a terminator.
        var length = BitConverter.ToInt16(bytes, ItemSize - 2 - 15 - 2);

        Assert.IsGreaterThanOrEqualTo(1, length);
        Assert.IsLessThanOrEqualTo(15, length);
        Assert.AreEqual(bytes.Length, ItemSize - 15 + length);
    }

    [TestMethod]
    public void SocketStartsWithEquipSerial()
    {
        var bytes = ToBytes(writer => new ItemSocketValue
        {
            EquipSerial = 0x0102030405060708,
            Socket = 0x11121314,
            Position = 0x21,
            Options = Enumerable.Range(0, 5).Select(_ => new ItemOption(0, 0)).ToList()
        }.Write(writer));

        Assert.HasCount(8 + 4 + 1 + 5 * 6, bytes);
        Assert.AreEqual(0x08, bytes[0]);
        Assert.AreEqual(0x14, bytes[8]);
        Assert.AreEqual(0x21, bytes[12]);
    }

    [TestMethod]
    public void BroachHasFifteenItems()
    {
        var bytes = ToBytes(writer => new ItemBroachValue { Serial = 1, Items = new int[15] }.Write(writer));

        Assert.HasCount(8 + 15 * 4, bytes);
    }

    [TestMethod]
    public void SocketAndBroachListsUseOneByteCount()
    {
        var sockets = ToBytes(writer => writer.Write(Array.Empty<ItemSocketValue>()));
        var broaches = ToBytes(writer => writer.Write(Array.Empty<ItemBroachValue>()));

        Assert.HasCount(1, sockets);
        Assert.HasCount(1, broaches);
    }

    [TestMethod]
    public void PostListsUseClientCountWidth()
    {
        var posts = ToBytes(writer => writer.Write(Array.Empty<PostValue>()));
        var accountPosts = ToBytes(writer => writer.Write(Array.Empty<PostAccountValue>()));

        Assert.HasCount(1, posts);
        Assert.HasCount(2, accountPosts);
    }

    [TestMethod]
    public void PostValueRoundTrips()
    {
        var bytes = ToBytes(writer => new PostValue
        {
            Serial = 1,
            Character = new PostCharacterValue { Character = 2, Class = 3, Name = "Name" },
            Title = "Title",
            Message = "Message",
            Money = 4,
            Items = Enumerable.Range(0, 5).Select(_ => CreateItem()).ToArray(),
            RegisteredTime = 5,
            Flag = 6,
            Type = 7,
            SubType = 8,
            RemainTime = 9,
            Event = 10,
            Sockets = [],
            Broaches = []
        }.Write(writer));

        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);

        var value = new PostValue(reader);

        Assert.AreEqual(stream.Length, stream.Position);
        Assert.AreEqual(1, value.Serial);
        Assert.AreEqual("Name", value.Character.Name);
        Assert.AreEqual("Title", value.Title);
        Assert.AreEqual("Message", value.Message);
        Assert.AreEqual(10, value.Event);
    }

    [TestMethod]
    public void TradeUpdateItemRequestMatchesClient()
    {
        using var stream = new MemoryStream([0x02, 0x03, 0x05, 0x00]);
        using var reader = new BinaryReader(stream);

        var request = new TradeUpdateItemRequest(reader);

        Assert.AreEqual(stream.Length, stream.Position);
        Assert.AreEqual(0x02, request.UpdateType);
        Assert.AreEqual((StorageGroup)0x03, request.Slot.Storage);
        Assert.AreEqual(5, request.Slot.Slot);
    }

    [TestMethod]
    public void PostRecvDelRequestMatchesClient()
    {
        using var stream = new MemoryStream(ToBytes(writer =>
        {
            writer.Write(7);
            writer.Write(8L);
            writer.Write((byte)9);
        }));
        using var reader = new BinaryReader(stream);

        var request = new PostRecvDelRequest(reader);

        Assert.AreEqual(stream.Length, stream.Position);
        Assert.AreEqual(7, request.Npc);
        Assert.AreEqual(8, request.Serial);
        Assert.AreEqual(9, request.State);
    }

    [TestMethod]
    public void PostReceiptResponseUsesClientLayout()
    {
        var bytes = ToBytes(new PostReceiptResponse { Serial = 1, PostFlag = 2, RemainTime = 3 }.Write);

        Assert.HasCount(8 + 1 + 8, bytes);
        Assert.AreEqual(2, bytes[8]);
        Assert.AreEqual(3, bytes[9]);
    }
}
