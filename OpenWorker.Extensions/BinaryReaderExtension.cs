using System.Runtime.CompilerServices;
using System.Text;

namespace OpenWorker.Extensions;

public static class BinaryReaderExtension
{
    #region Another Strings

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadAsciiStringWithoutTerminator(this BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);
        var value = Encoding.ASCII.GetString(bytes);

        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadUtf8AsciiStringWithoutTerminator(this BinaryReader reader)
    {
        var length = reader.ReadInt16();
        var bytes = reader.ReadBytes(length);
        var value = Encoding.ASCII.GetString(bytes);

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadUtf8UnicodeStringWithoutTerminator(this BinaryReader reader)
    {
        var length = reader.ReadInt16();
        var bytes = reader.ReadBytes(length);
        var value = Encoding.Unicode.GetString(bytes);

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadUtf16UnicodeStringWithoutTerminator(this BinaryReader reader, int max = 0)
    {
        var length = reader.ReadInt16() * 2;
        var bytes = reader.ReadBytes(length);
        var value = Encoding.Unicode.GetString(bytes);

        return value;
    }

    #endregion Another Strings

    #region Enumerable

    public static IEnumerable<int> ReadInt32AsEnumerable(this BinaryReader reader, int count)
    {
        return Enumerable.Repeat(0, count).Select(_ => reader.ReadInt32());
    }

    public static IEnumerable<short> ReadUInt16AsEnumerable(this BinaryReader reader, int count)
    {
        return Enumerable.Repeat(0, count).Select(_ => reader.ReadInt16());
    }

    public static IEnumerable<byte> ReadByteAsEnumerable(this BinaryReader reader, int count)
    {
        return Enumerable.Repeat(0, count).Select(_ => reader.ReadByte());
    }

    // public static IEnumerable<BoosterMazeEffectType> ReadBoosterEffectTypeAsEnumerable(this BinaryReader reader, int count)
    // {
    //     return Enumerable.Repeat(0, count).Select(_ => (BoosterMazeEffectType)reader.ReadByte());
    // }
    //
    // public static IEnumerable<string> ReadByteLengthUnicodeStringAsEnumerable(this BinaryReader reader, int count)
    // {
    //     return Enumerable.Repeat(0, count).Select(_ => reader.ReadUTF16UnicodeString());
    // }

    #endregion Enumerable

    #region Arrays

    public static int[] ReadInt32AsArray(this BinaryReader reader, int count)
    {
        return reader.ReadInt32AsEnumerable(count).ToArray();
    }

    public static short[] ReadUInt16AsArray(this BinaryReader reader, int count)
    {
        return reader.ReadUInt16AsEnumerable(count).ToArray();
    }

    public static byte[] ReadByteAsArray(this BinaryReader reader, int count)
    {
        return Enumerable.Repeat(0, count).Select(_ => reader.ReadByte()).ToArray();
    }

    public static float[] ReadSingleAsArray(this BinaryReader reader, int count)
    {
        return Enumerable.Repeat(0, count).Select(_ => reader.ReadSingle()).ToArray();
    }

    // public static string[] ReadByteLengthUnicodeStringAsArray(this BinaryReader reader, int count)
    // {
    //     return reader.ReadByteLengthUnicodeStringAsEnumerable(count).ToArray();
    // }

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
        return reader.ReadUnicodeString(length, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadUtf16UnicodeString(this BinaryReader reader, int max = 0)
    {
        var length = reader.ReadInt16() * 2;
        return reader.ReadUnicodeString(length, max);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ReadUnicodeString(this BinaryReader reader, int length, int max = 0)
    {
        var bytes = reader.ReadBytes(length);
        var value = Encoding.Unicode.GetString(bytes.AsSpan(0, length - 1) /* skip null-terminator */);

        return value;
    }

    #endregion Strings
}