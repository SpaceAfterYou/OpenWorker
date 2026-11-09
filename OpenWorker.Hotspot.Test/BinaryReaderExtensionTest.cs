using OpenWorker.Extensions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class BinaryReaderExtensionTest
{
    private static BinaryReader Reader(params byte[] bytes) => new(new MemoryStream(bytes));

    [TestMethod]
    public void ReadsAStringAndDropsOneByteOfTheTerminator()
    {
        // Three bytes follow the length; the last one is the terminator.
        using var utf8 = Reader(0x03, 0x00, 0x41, 0x00, 0x00);

        // Twice the length in bytes: four.
        using var utf16 = Reader(0x02, 0x00, 0x41, 0x00, 0x00, 0x00);

        Assert.AreEqual("A", utf8.ReadUtf8UnicodeString());
        Assert.StartsWith("A", utf16.ReadUtf16UnicodeString());
        Assert.AreEqual(utf16.BaseStream.Length, utf16.BaseStream.Position);
    }

    [TestMethod]
    public void AZeroLengthIsInvalidData()
    {
        using var utf8 = Reader(0x00, 0x00);
        using var utf16 = Reader(0x00, 0x00);

        Assert.ThrowsExactly<InvalidDataException>(() => utf8.ReadUtf8UnicodeString());
        Assert.ThrowsExactly<InvalidDataException>(() => utf16.ReadUtf16UnicodeString());
    }

    [TestMethod]
    public void ANegativeLengthIsInvalidData()
    {
        using var utf8 = Reader(0xFF, 0xFF, 0x41, 0x00);
        using var utf16 = Reader(0x00, 0x80, 0x41, 0x00);

        Assert.ThrowsExactly<InvalidDataException>(() => utf8.ReadUtf8UnicodeString());
        Assert.ThrowsExactly<InvalidDataException>(() => utf16.ReadUtf16UnicodeString());
    }

    [TestMethod]
    public void AStringThatTheStreamCannotFillIsInvalidData()
    {
        // The length promises five bytes and three are left; and four are left, which used to pass.
        using var farShort = Reader(0x05, 0x00, 0x41, 0x00, 0x00);
        using var oneShort = Reader(0x05, 0x00, 0x41, 0x00, 0x42, 0x00);

        Assert.ThrowsExactly<InvalidDataException>(() => farShort.ReadUtf8UnicodeString());
        Assert.ThrowsExactly<InvalidDataException>(() => oneShort.ReadUtf8UnicodeString());
    }

    /// <summary>
    /// A count is checked against what the stream still holds before anything is made for it: the client cannot make
    /// the server allocate what it never sent.
    /// </summary>
    [TestMethod]
    public void ALengthPastTheEndOfThePacketIsRefusedBeforeAnythingIsAllocated()
    {
        // The largest count there is, and a few bytes after it.
        using var reader = Reader(0xFF, 0xFF, 0xFF, 0x7F, 0x01, 0x02, 0x03, 0x04);

        var count = reader.ReadInt32();
        var before = GC.GetAllocatedBytesForCurrentThread();

        var refused = Assert.ThrowsExactly<InvalidDataException>(() => reader.ReadInt32AsArray(count));

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // The exception and its message, nothing like the 8 GiB the count asks for.
        Assert.IsLessThan(64 * 1024, allocated);
        StringAssert.Contains(refused.Message, nameof(BinaryReaderExtension.ReadInt32AsArray));
        StringAssert.Contains(refused.Message, int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual(4, reader.BaseStream.Position, "Nothing is read for a count that is refused.");
    }
}
