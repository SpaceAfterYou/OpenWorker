using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Rewards.Types;
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
    private static byte[] Written(IResponseHotspotMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

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
}
