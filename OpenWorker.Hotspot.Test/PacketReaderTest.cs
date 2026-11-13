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
        Assert.ThrowsExactly<InvalidDataException>(() => new PacketReader([0x11, 0x00, 0x41, 0x00]).ReadUtf16UnicodeString());
        Assert.ThrowsExactly<EndOfStreamException>(() => new PacketReader([0x01, 0x02, 0x03]).ReadInt32());
    }

    /// <summary>
    /// What the writer puts in a format, the reader of that format takes back whole: the same text, and not a byte more or
    /// less.
    /// </summary>
    [TestMethod]
    public void EveryStringFormatReadsBackWhatItWrites()
    {
        foreach (var format in Enum.GetValues<WireStringFormat>())
        {
            var unicode = format == WireStringFormat.Utf16Unicode;

            foreach (var text in unicode ? new[] { string.Empty, "Plugcash", "セット" } : [string.Empty, "Plugcash"])
            {
                var bytes = Write(format, text);
                var reader = new PacketReader(bytes);

                Assert.AreEqual(text, Read(ref reader, format, text.Length), $"{format} of \"{text}\"");
                Assert.AreEqual(0, reader.Remaining, $"{format} of \"{text}\" leaves bytes unread");
            }
        }
    }

    /// <summary>
    /// The client writes a text as a length of twice the characters plus one, the characters and one zero byte. A length it
    /// never writes is refused as malformed: zero or negative, even, past the end of the packet, a last byte that is not
    /// zero, a zero character inside the text.
    /// </summary>
    [TestMethod]
    public void ATextLengthTheClientNeverWritesIsRefused()
    {
        byte[][] packets =
        [
            [0x00, 0x00],
            [0xFF, 0xFF, 0x41, 0x00, 0x00],
            [0x02, 0x00, 0x41, 0x00],
            [0x05, 0x00, 0x41, 0x00, 0x00],
            [0x03, 0x00, 0x41, 0x00, 0x41],
            [0x05, 0x00, 0x00, 0x00, 0x41, 0x00, 0x00]
        ];

        foreach (var packet in packets)
        {
            Assert.ThrowsExactly<InvalidDataException>(() => new PacketReader(packet).ReadUtf16UnicodeString(),
                Convert.ToHexString(packet));
        }
    }

    private static byte[] Write(WireStringFormat format, string text)
    {
        var writer = new PacketWriter(System.Buffers.ArrayPool<byte>.Shared, 64, 1024);

        try
        {
            switch (format)
            {
                case WireStringFormat.Ascii: writer.WriteAsciiString(text); break;
                case WireStringFormat.AsciiWithoutTerminator: writer.WriteAsciiStringWithoutTerminator(text); break;
                case WireStringFormat.Utf8Ascii: writer.WriteUtf8AsciiString(text); break;
                case WireStringFormat.Utf8AsciiWithoutTerminator: writer.WriteUtf8AsciiStringWithoutTerminator(text); break;
                case WireStringFormat.Utf16Unicode: writer.WriteUtf16UnicodeString(text); break;
                default: Assert.Fail($"{format} has no writer in this test"); break;
            }

            return writer.Written.ToArray();
        }

        finally
        {
            writer.Dispose();
        }
    }

    private static string Read(ref PacketReader reader, WireStringFormat format, int length)
    {
        return format switch
        {
            WireStringFormat.Ascii => reader.ReadAsciiString(),
            WireStringFormat.AsciiWithoutTerminator => reader.ReadAsciiStringWithoutTerminator(length),
            WireStringFormat.Utf8Ascii => reader.ReadUtf8AsciiString(),
            WireStringFormat.Utf8AsciiWithoutTerminator => reader.ReadUtf8AsciiStringWithoutTerminator(),
            WireStringFormat.Utf16Unicode => reader.ReadUtf16UnicodeString(),
            _ => throw new AssertFailedException($"{format} has no reader in this test")
        };
    }
}
