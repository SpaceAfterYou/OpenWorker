using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class MessageHeaderTest
{
    private static MessageHeader Read(ushort length)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((ushort)0);
        writer.Write(length);
        writer.Write((byte)1);

        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return new MessageHeader(reader);
    }

    [TestMethod]
    [DataRow((ushort)5, 0)]
    [DataRow((ushort)7, 2)]
    [DataRow((ushort)32768, 32763)]
    [DataRow(ushort.MaxValue, MessageHeader.MaxContentLength)]
    public void ReadsContentLength(ushort length, int expected)
    {
        Assert.AreEqual(expected, Read(length).ContentLength);
    }

    [TestMethod]
    [DataRow((ushort)0)]
    [DataRow((ushort)4)]
    public void RejectsLengthSmallerThanHeader(ushort length)
    {
        Assert.ThrowsExactly<InvalidDataException>(() => Read(length));
    }

    [TestMethod]
    [DataRow(-1L)]
    [DataRow(MessageHeader.MaxContentLength + 1L)]
    public void RejectsContentLengthOutOfRange(long length)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MessageHeader(length));
    }

    [TestMethod]
    public void WritesLengthIncludingHeader()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        new MessageHeader(MessageHeader.MaxContentLength).ToBinary(writer);

        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        Assert.AreEqual(MessageHeader.MaxContentLength, new MessageHeader(reader).ContentLength);
    }
}
