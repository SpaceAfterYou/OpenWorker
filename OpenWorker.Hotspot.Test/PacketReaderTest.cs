using System.Globalization;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class PacketReaderTest
{
    // The largest count there is, then a few bytes: the client claims 2^31 elements and sends four bytes.
    private static readonly byte[] Hostile = [0xFF, 0xFF, 0xFF, 0x7F, 0x01, 0x02, 0x03, 0x04];

    private static int[] ReadArray(byte[] packet)
    {
        var reader = new PacketReader(packet);

        return reader.ReadInt32Array(reader.ReadInt32());
    }

    private static int ReadList(byte[] packet)
    {
        var reader = new PacketReader(packet);

        // What a generated reader does for a list with an int prefix of elements of four bytes.
        return reader.ReadLength(WireLength.Signed32, sizeof(int));
    }

    /// <summary>
    /// A count is checked against what the packet still holds before anything is made for it: the client cannot make the
    /// server allocate what it never sent, and the refusal is <see cref="InvalidDataException"/>, which the error budget
    /// counts as a malformed packet.
    /// </summary>
    [TestMethod]
    public void AHostileLengthIsRefusedBeforeAnythingIsAllocated()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();

        var array = Assert.ThrowsExactly<InvalidDataException>(() => ReadArray(Hostile));
        var list = Assert.ThrowsExactly<InvalidDataException>(() => ReadList(Hostile));

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Two exceptions and their messages, nothing like the 8 GiB the count asks for.
        Assert.IsLessThan(64 * 1024, allocated);
        StringAssert.Contains(array.Message, nameof(PacketReader.ReadInt32Array));
        StringAssert.Contains(array.Message, int.MaxValue.ToString(CultureInfo.InvariantCulture));
        StringAssert.Contains(list.Message, "4 are left");

        // A string that claims more than the packet holds, and a value past the end, are refused too.
        Assert.ThrowsExactly<InvalidDataException>(() => new PacketReader([0x10, 0x00, 0x41, 0x00]).ReadUtf8UnicodeString());
        Assert.ThrowsExactly<EndOfStreamException>(() => new PacketReader([0x01, 0x02, 0x03]).ReadInt32());
    }
}
