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
    public void AStringThatTheStreamCannotFillIsAnEarlyEnd()
    {
        // The length promises five bytes and three are left; and four are left, which used to pass.
        using var farShort = Reader(0x05, 0x00, 0x41, 0x00, 0x00);
        using var oneShort = Reader(0x05, 0x00, 0x41, 0x00, 0x42, 0x00);

        Assert.ThrowsExactly<EndOfStreamException>(() => farShort.ReadUtf8UnicodeString());
        Assert.ThrowsExactly<EndOfStreamException>(() => oneShort.ReadUtf8UnicodeString());
    }
}
