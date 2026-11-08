using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenWorker.Wire;

/// <summary>
/// Reads a packet of the client, little endian, from a span. The client is hostile: every read checks what is left. A
/// value past the end is <see cref="EndOfStreamException"/>, as <c>BinaryReader</c> gives; a length or a count that the
/// packet cannot hold is <see cref="InvalidDataException"/>, refused before anything is allocated for it. Both count as a
/// malformed packet against the error budget of the session.<br/>
/// The readers of strings and arrays have the layout of the helpers of <c>BinaryReader</c> with the same names.
/// </summary>
public ref struct PacketReader
{
    private readonly ReadOnlySpan<byte> _span;
    private int _position;

    public PacketReader(ReadOnlySpan<byte> span)
    {
        _span = span;
    }

    /// <summary>
    /// The bytes read so far.
    /// </summary>
    public readonly int Position => _position;

    /// <summary>
    /// The bytes not read yet.
    /// </summary>
    public readonly int Remaining => _span.Length - _position;

#region Primitives

    public byte ReadByte()
    {
        return Take(sizeof(byte))[0];
    }

    public sbyte ReadSByte()
    {
        return (sbyte)ReadByte();
    }

    /// <summary>
    /// One byte; anything but zero is true, as <c>BinaryReader.ReadBoolean</c>.
    /// </summary>
    public bool ReadBoolean()
    {
        return ReadByte() != 0;
    }

    public short ReadInt16()
    {
        return BinaryPrimitives.ReadInt16LittleEndian(Take(sizeof(short)));
    }

    public ushort ReadUInt16()
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(Take(sizeof(ushort)));
    }

    public int ReadInt32()
    {
        return BinaryPrimitives.ReadInt32LittleEndian(Take(sizeof(int)));
    }

    public uint ReadUInt32()
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(Take(sizeof(uint)));
    }

    public long ReadInt64()
    {
        return BinaryPrimitives.ReadInt64LittleEndian(Take(sizeof(long)));
    }

    public ulong ReadUInt64()
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(Take(sizeof(ulong)));
    }

    public float ReadSingle()
    {
        return BinaryPrimitives.ReadSingleLittleEndian(Take(sizeof(float)));
    }

    public double ReadDouble()
    {
        return BinaryPrimitives.ReadDoubleLittleEndian(Take(sizeof(double)));
    }

    /// <summary>
    /// X, Y and Z, three floats.
    /// </summary>
    public Vector3 ReadVector3()
    {
        var span = Take(3 * sizeof(float));

        return new Vector3(
            BinaryPrimitives.ReadSingleLittleEndian(span),
            BinaryPrimitives.ReadSingleLittleEndian(span[4..]),
            BinaryPrimitives.ReadSingleLittleEndian(span[8..]));
    }

    /// <summary>
    /// Seconds since 1970 as a 64-bit number, the layout <see cref="PacketWriter.WriteDateTime"/> writes; the time is UTC.
    /// </summary>
    public DateTime ReadDateTime()
    {
        var seconds = ReadInt64();

        if (seconds < DateTimeOffset.MinValue.ToUnixTimeSeconds() || seconds > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
        {
            ThrowBadValue(nameof(ReadDateTime), seconds);
        }

        return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
    }

    /// <summary>
    /// Seconds since 1970 as a 64-bit number, the layout <see cref="PacketWriter.WriteDateTimeOffset"/> writes.
    /// </summary>
    public DateTimeOffset ReadDateTimeOffset()
    {
        return new DateTimeOffset(ReadDateTime());
    }

    /// <summary>
    /// A 32-bit count of seconds.
    /// </summary>
    public TimeSpan ReadTimeInSeconds32()
    {
        return TimeSpan.FromSeconds(ReadInt32());
    }

    /// <summary>
    /// The next <paramref name="count"/> bytes, without a copy; they live as long as the packet.
    /// </summary>
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        CheckLength(count, sizeof(byte), nameof(ReadBytes));

        return Take(count);
    }

    /// <summary>
    /// Passes over <paramref name="count"/> bytes.
    /// </summary>
    public void Skip(int count)
    {
        ReadBytes(count);
    }

#endregion Primitives

#region Arrays

    /// <summary>
    /// The count of a list in its prefix, checked against what is left: every element needs at least
    /// <paramref name="size"/> bytes.
    /// </summary>
    public int ReadLength(WireLength prefix, int size)
    {
        int count = prefix switch
        {
            WireLength.Byte => ReadByte(),
            WireLength.Signed16 => ReadInt16(),
            WireLength.Unsigned16 => ReadUInt16(),
            WireLength.Signed32 => ReadInt32(),
            _ => throw new ArgumentOutOfRangeException(nameof(prefix), prefix, "Not a prefix of a list.")
        };

        CheckLength(count, size, nameof(ReadLength));

        return count;
    }

    public byte[] ReadByteArray(int count)
    {
        return ReadBytes(count).ToArray();
    }

    public int[] ReadInt32Array(int count)
    {
        CheckLength(count, sizeof(int), nameof(ReadInt32Array));

        var values = new int[count];

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = ReadInt32();
        }

        return values;
    }

    public short[] ReadInt16Array(int count)
    {
        CheckLength(count, sizeof(short), nameof(ReadInt16Array));

        var values = new short[count];

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = ReadInt16();
        }

        return values;
    }

    public float[] ReadSingleArray(int count)
    {
        CheckLength(count, sizeof(float), nameof(ReadSingleArray));

        var values = new float[count];

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = ReadSingle();
        }

        return values;
    }

#endregion Arrays

#region Strings

    /// <summary>
    /// <paramref name="length"/> characters in ASCII, no terminator.
    /// </summary>
    public string ReadAsciiStringWithoutTerminator(int length)
    {
        CheckLength(length, 1, nameof(ReadAsciiStringWithoutTerminator));

        return Encoding.ASCII.GetString(Take(length));
    }

    /// <summary>
    /// ASCII up to a zero byte, which is read and dropped. A packet that ends first is invalid.
    /// </summary>
    public string ReadAsciiString()
    {
        var end = _span[_position..].IndexOf((byte)0);

        if (end < 0)
        {
            ThrowNoTerminator(nameof(ReadAsciiString), Remaining);
        }

        var value = Encoding.ASCII.GetString(Take(end));

        _position++;

        return value;
    }

    /// <summary>
    /// A 16-bit length, the characters plus one; the characters in ASCII; one zero byte.
    /// </summary>
    public string ReadUtf8AsciiString()
    {
        int length = ReadInt16();

        return ReadTerminated(length, length - 1, Encoding.ASCII, nameof(ReadUtf8AsciiString));
    }

    /// <summary>
    /// A 16-bit length, twice the characters plus one; the characters in ASCII; one zero byte.
    /// </summary>
    public string ReadUtf16AsciiString()
    {
        int length = ReadInt16();

        if (length <= 0 || length % 2 == 0)
        {
            ThrowBadStringLength(nameof(ReadUtf16AsciiString), length);
        }

        return ReadTerminated((length - 1) / 2 + 1, (length - 1) / 2, Encoding.ASCII, nameof(ReadUtf16AsciiString));
    }

    /// <summary>
    /// A 16-bit length, the characters; the characters in ASCII.
    /// </summary>
    public string ReadUtf8AsciiStringWithoutTerminator()
    {
        int length = ReadInt16();

        CheckLength(length, 1, nameof(ReadUtf8AsciiStringWithoutTerminator));

        return Encoding.ASCII.GetString(Take(length));
    }

    /// <summary>
    /// A 16-bit length in bytes, then UTF-16 of that many bytes.
    /// </summary>
    public string ReadUtf8UnicodeStringWithoutTerminator()
    {
        int length = ReadInt16();

        CheckLength(length, 1, nameof(ReadUtf8UnicodeStringWithoutTerminator));

        return Encoding.Unicode.GetString(Take(length));
    }

    /// <summary>
    /// A 16-bit count of characters, then the characters in UTF-16.
    /// </summary>
    public string ReadUtf16UnicodeStringWithoutTerminator()
    {
        int length = ReadInt16();

        CheckLength(length, 2, nameof(ReadUtf16UnicodeStringWithoutTerminator));

        return Encoding.Unicode.GetString(Take(length * 2));
    }

    /// <summary>
    /// A 16-bit length in bytes that counts a one-byte terminator, then UTF-16 and the terminator, which is dropped.
    /// </summary>
    public string ReadUtf8UnicodeString()
    {
        int length = ReadInt16();

        return ReadTerminated(length, length - 1, Encoding.Unicode, nameof(ReadUtf8UnicodeString));
    }

    /// <summary>
    /// A 16-bit count of characters that counts a two-byte terminator, then UTF-16 and the terminator, which is dropped.
    /// </summary>
    /// <remarks>The reader of <c>BinaryReader</c> drops one byte of the terminator and leaves the other in the string, as a
    /// U+FFFD at its end; this one drops both. The bytes it reads are the same.</remarks>
    public string ReadUtf16UnicodeString()
    {
        var length = ReadInt16() * 2;

        return ReadTerminated(length, length - 2, Encoding.Unicode, nameof(ReadUtf16UnicodeString));
    }

    /// <summary>
    /// <paramref name="length"/> bytes, of which the first <paramref name="text"/> are the string and the rest its
    /// terminator.
    /// </summary>
    private string ReadTerminated(int length, int text, Encoding encoding, string method)
    {
        // The length comes from the client, and a string with a terminator is never shorter than one.
        if (length <= 0)
        {
            ThrowBadStringLength(method, length);
        }

        CheckLength(length, 1, method);

        return encoding.GetString(Take(length)[..Math.Max(text, 0)]);
    }

#endregion Strings

#region Checks

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> Take(int count)
    {
        var position = _position;

        if ((uint)count > (uint)(_span.Length - position))
        {
            ThrowEnd(count, _span.Length - position);
        }

        _position = position + count;

        return _span.Slice(position, count);
    }

    /// <summary>
    /// A count read from the packet is not negative, and the packet still holds that many elements of
    /// <paramref name="size"/> bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void CheckLength(int count, int size, string method)
    {
        if (count < 0)
        {
            ThrowNegativeLength(method, count);
        }

        if ((long)count * size > Remaining)
        {
            ThrowLengthPastTheEnd(method, count, size, Remaining);
        }
    }

    [DoesNotReturn]
    private static void ThrowEnd(int count, int left)
    {
        throw new EndOfStreamException($"The packet ends: {count} bytes are needed and {left} are left.");
    }

    [DoesNotReturn]
    private static void ThrowNegativeLength(string method, int count)
    {
        throw new InvalidDataException($"{method}: the length {count} is negative.");
    }

    [DoesNotReturn]
    private static void ThrowLengthPastTheEnd(string method, int count, int size, int left)
    {
        throw new InvalidDataException($"{method}: the length {count} needs {(long)count * size} bytes and {left} are left.");
    }

    [DoesNotReturn]
    private static void ThrowBadStringLength(string method, int length)
    {
        throw new InvalidDataException($"{method}: a string of {length} bytes cannot hold its terminator.");
    }

    [DoesNotReturn]
    private static void ThrowNoTerminator(string method, int left)
    {
        throw new InvalidDataException($"{method}: the {left} bytes left hold no terminator.");
    }

    [DoesNotReturn]
    private static void ThrowBadValue(string method, long value)
    {
        throw new InvalidDataException($"{method}: {value} is out of range.");
    }

#endregion Checks
}
