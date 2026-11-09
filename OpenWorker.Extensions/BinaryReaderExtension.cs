using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenWorker.Extensions;

public static class BinaryReaderExtension
{
#region Another Strings

    extension(BinaryReader reader)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAsciiStringWithoutTerminator(int length)
        {
            CheckLength(reader, length, 1, nameof(ReadAsciiStringWithoutTerminator));

            var bytes = reader.ReadBytes(length);
            var value = Encoding.ASCII.GetString(bytes);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUtf8AsciiStringWithoutTerminator()
        {
            var length = reader.ReadInt16();

            CheckLength(reader, length, 1, nameof(ReadUtf8AsciiStringWithoutTerminator));

            var bytes = reader.ReadBytes(length);
            var value = Encoding.ASCII.GetString(bytes);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUtf8UnicodeStringWithoutTerminator()
        {
            var length = reader.ReadInt16();

            CheckLength(reader, length, 1, nameof(ReadUtf8UnicodeStringWithoutTerminator));

            var bytes = reader.ReadBytes(length);
            var value = Encoding.Unicode.GetString(bytes);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUtf16UnicodeStringWithoutTerminator(int max = 0)
        {
            // A count of characters, two bytes each.
            var length = reader.ReadInt16();

            CheckLength(reader, length, 2, nameof(ReadUtf16UnicodeStringWithoutTerminator));

            var bytes = reader.ReadBytes(length * 2);
            var value = Encoding.Unicode.GetString(bytes);

            return value;
        }
    }

#endregion Another Strings

#region Enumerable

    // Read at once, not as they are enumerated: the count is checked before the first element, and the elements are
    // read while the reader still points at them.

    public static IEnumerable<int> ReadInt32AsEnumerable(this BinaryReader reader, int count)
    {
        return reader.ReadInt32AsArray(count);
    }

    public static IEnumerable<short> ReadUInt16AsEnumerable(this BinaryReader reader, int count)
    {
        return reader.ReadUInt16AsArray(count);
    }

    public static IEnumerable<byte> ReadByteAsEnumerable(this BinaryReader reader, int count)
    {
        return reader.ReadByteAsArray(count);
    }

#endregion Enumerable

#region Arrays

    public static int[] ReadInt32AsArray(this BinaryReader reader, int count)
    {
        CheckLength(reader, count, sizeof(int), nameof(ReadInt32AsArray));

        var values = new int[count];

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = reader.ReadInt32();
        }

        return values;
    }

    public static short[] ReadUInt16AsArray(this BinaryReader reader, int count)
    {
        CheckLength(reader, count, sizeof(short), nameof(ReadUInt16AsArray));

        var values = new short[count];

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = reader.ReadInt16();
        }

        return values;
    }

    public static byte[] ReadByteAsArray(this BinaryReader reader, int count)
    {
        CheckLength(reader, count, sizeof(byte), nameof(ReadByteAsArray));

        var values = new byte[count];

        reader.BaseStream.ReadExactly(values);

        return values;
    }

    public static float[] ReadSingleAsArray(this BinaryReader reader, int count)
    {
        CheckLength(reader, count, sizeof(float), nameof(ReadSingleAsArray));

        var values = new float[count];

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = reader.ReadSingle();
        }

        return values;
    }

#endregion Arrays

#region Core.Time

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan ReadTimeInSeconds32(this BinaryReader reader)
    {
        return TimeSpan.FromSeconds(reader.ReadInt32());
    }

#endregion Core.Time

#region Strings

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadUtf8UnicodeString(this BinaryReader reader, int max = 0)
    {
        var length = reader.ReadInt16();
        return reader.ReadUnicodeString(length, nameof(ReadUtf8UnicodeString));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadUtf16UnicodeString(this BinaryReader reader, int max = 0)
    {
        var length = reader.ReadInt16() * 2;
        return reader.ReadUnicodeString(length, nameof(ReadUtf16UnicodeString));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ReadUnicodeString(this BinaryReader reader, int length, string method)
    {
        // The length comes from the client, and a string with a terminator is never shorter than one.
        if (length <= 0)
        {
            ThrowBadStringLength(method, length);
        }

        CheckLength(reader, length, 1, method);

        var bytes = reader.ReadBytes(length);

        // ReadBytes hands back what is left instead of failing when the packet ends early.
        if (bytes.Length < length)
        {
            ThrowStringCutShort(length, bytes.Length);
        }

        var value = Encoding.Unicode.GetString(bytes.AsSpan(0, length - 1) /* skip null-terminator */);

        return value;
    }

    [DoesNotReturn]
    private static void ThrowBadStringLength(string method, int length)
    {
        throw new InvalidDataException($"{method}: a string of {length} bytes cannot hold its terminator.");
    }

    [DoesNotReturn]
    private static void ThrowStringCutShort(int length, int available)
    {
        throw new EndOfStreamException($"A string of {length} bytes is cut short after {available}.");
    }

#endregion Strings

#region Lengths

    /// <summary>
    /// A count read from the network is checked before anything is allocated or read for it: it is not negative, and
    /// the stream still holds that many elements of <paramref name="size"/> bytes. A stream that cannot tell its
    /// length is only checked for a negative count, and ends early on a count it cannot fill.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckLength(BinaryReader reader, int count, int size, string method)
    {
        if (count < 0)
        {
            ThrowNegativeLength(method, count);
        }

        var stream = reader.BaseStream;

        if (stream.CanSeek && (long)count * size > stream.Length - stream.Position)
        {
            ThrowLengthPastTheEnd(method, count, size, stream.Length - stream.Position);
        }
    }

    [DoesNotReturn]
    private static void ThrowNegativeLength(string method, int count)
    {
        throw new InvalidDataException($"{method}: the length {count} is negative.");
    }

    [DoesNotReturn]
    private static void ThrowLengthPastTheEnd(string method, int count, int size, long left)
    {
        throw new InvalidDataException($"{method}: the length {count} needs {(long)count * size} bytes and {left} are left.");
    }

#endregion Lengths
}
