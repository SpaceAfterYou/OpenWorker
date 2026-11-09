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

    /// <summary>
    /// The length of a frame counts its header of five bytes: a frame of the header alone has no content, a shorter one
    /// is refused, and the largest length a <c>u16</c> holds is read without a sign.
    /// </summary>
    [TestMethod]
    public void ReadsContentLength()
    {
        Assert.AreEqual(0, Read(5).ContentLength);
        Assert.AreEqual(MessageHeader.MaxContentLength, Read(ushort.MaxValue).ContentLength);
        Assert.ThrowsExactly<InvalidDataException>(() => Read(4));
    }

    [TestMethod]
    public void RejectsContentLengthOutOfRange()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MessageHeader(-1L));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MessageHeader(MessageHeader.MaxContentLength + 1L));
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
