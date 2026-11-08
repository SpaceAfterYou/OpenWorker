using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenWorker.Wire;

/// <summary>
/// Writes a message in the layout the client reads, little endian, straight into a span: the array of a frame, or any
/// buffer a caller gives. It never throws for a lack of room: a write that does not fit is dropped and
/// <see cref="Overflowed"/> says so, and so does every write after it; the caller decides what a cut message is worth (the
/// frame encoder drops it with a warning).<br/>
/// Over an array of a pool it grows by renting a larger one, up to its limit; over a plain span the span is the limit.
/// Every method has the layout of the helper of <c>BinaryWriter</c> with the same name, so a message moves from one to the
/// other without a byte of difference.
/// </summary>
public ref struct PacketWriter
{
    private readonly ArrayPool<byte>? _pool;
    private readonly int _limit;

    private byte[]? _array;
    private Span<byte> _span;
    private int _position;
    private bool _overflowed;

    /// <summary>
    /// A writer over a buffer that does not grow: past its end the writer overflows.
    /// </summary>
    public PacketWriter(Span<byte> buffer)
    {
        _span = buffer;
        _limit = buffer.Length;
    }

    /// <summary>
    /// A writer over an array of <paramref name="pool"/> that grows up to <paramref name="limit"/> bytes. The first
    /// <paramref name="start"/> bytes are left for the caller (the header of a frame). Give the array back with
    /// <see cref="Dispose"/>, or take it with <see cref="Detach"/>.
    /// </summary>
    public PacketWriter(ArrayPool<byte> pool, int capacity, int limit, int start = 0)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, start);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, capacity);

        _pool = pool;
        _limit = limit;
        _array = pool.Rent(capacity);
        _span = Usable(_array, limit);
        _position = start;
    }

    /// <summary>
    /// The bytes written so far, the ones left at the start included.
    /// </summary>
    public readonly int Position => _position;

    /// <summary>
    /// A write did not fit: the message is cut and must not be sent.
    /// </summary>
    public readonly bool Overflowed => _overflowed;

    /// <summary>
    /// What is written so far, the bytes left at the start included.
    /// </summary>
    public readonly ReadOnlySpan<byte> Written => _span[.._position];

    /// <summary>
    /// Hands the array of the pool over to the caller, who returns it to the pool; the writer is left empty.
    /// </summary>
    public byte[] Detach(out int length)
    {
        var array = _array ?? throw new InvalidOperationException("A writer over a span has no array to hand over.");

        length = _position;

        _array = null;
        _span = default;
        _position = 0;

        return array;
    }

    /// <summary>
    /// Gives the array back to its pool, unless it was handed over.
    /// </summary>
    public void Dispose()
    {
        if (_array is not null)
        {
            _pool!.Return(_array);
        }

        _array = null;
        _span = default;
        _position = 0;
    }

#region Primitives

    public void WriteByte(byte value)
    {
        var span = Take(sizeof(byte));

        if (span.Length == sizeof(byte))
        {
            span[0] = value;
        }
    }

    public void WriteSByte(sbyte value)
    {
        WriteByte((byte)value);
    }

    /// <summary>
    /// One byte, 1 or 0, as <c>BinaryWriter.Write(bool)</c>.
    /// </summary>
    public void WriteBoolean(bool value)
    {
        WriteByte(value ? (byte)1 : (byte)0);
    }

    public void WriteInt16(short value)
    {
        var span = Take(sizeof(short));

        if (span.Length == sizeof(short))
        {
            BinaryPrimitives.WriteInt16LittleEndian(span, value);
        }
    }

    public void WriteUInt16(ushort value)
    {
        var span = Take(sizeof(ushort));

        if (span.Length == sizeof(ushort))
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        }
    }

    public void WriteInt32(int value)
    {
        var span = Take(sizeof(int));

        if (span.Length == sizeof(int))
        {
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
        }
    }

    public void WriteUInt32(uint value)
    {
        var span = Take(sizeof(uint));

        if (span.Length == sizeof(uint))
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        }
    }

    public void WriteInt64(long value)
    {
        var span = Take(sizeof(long));

        if (span.Length == sizeof(long))
        {
            BinaryPrimitives.WriteInt64LittleEndian(span, value);
        }
    }

    public void WriteUInt64(ulong value)
    {
        var span = Take(sizeof(ulong));

        if (span.Length == sizeof(ulong))
        {
            BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        }
    }

    public void WriteSingle(float value)
    {
        var span = Take(sizeof(float));

        if (span.Length == sizeof(float))
        {
            BinaryPrimitives.WriteSingleLittleEndian(span, value);
        }
    }

    public void WriteDouble(double value)
    {
        var span = Take(sizeof(double));

        if (span.Length == sizeof(double))
        {
            BinaryPrimitives.WriteDoubleLittleEndian(span, value);
        }
    }

    /// <summary>
    /// X, Y and Z, three floats.
    /// </summary>
    public void WriteVector3(Vector3 value)
    {
        var span = Take(3 * sizeof(float));

        if (span.Length == 3 * sizeof(float))
        {
            BinaryPrimitives.WriteSingleLittleEndian(span, value.X);
            BinaryPrimitives.WriteSingleLittleEndian(span[4..], value.Y);
            BinaryPrimitives.WriteSingleLittleEndian(span[8..], value.Z);
        }
    }

    /// <summary>
    /// Seconds since 1970 as a 64-bit number; a time that is not UTC is taken as local, as the helper of
    /// <c>BinaryWriter</c> does.
    /// </summary>
    public void WriteDateTime(DateTime value)
    {
        WriteInt64(new DateTimeOffset(value).ToUnixTimeSeconds());
    }

    /// <summary>
    /// Seconds since 1970 as a 64-bit number.
    /// </summary>
    public void WriteDateTimeOffset(DateTimeOffset value)
    {
        WriteInt64(value.ToUnixTimeSeconds());
    }

    /// <summary>
    /// Whole seconds as a 32-bit number, the fraction dropped.
    /// </summary>
    public void WriteTimeInSeconds32(TimeSpan value)
    {
        WriteInt32((int)value.TotalSeconds);
    }

    /// <summary>
    /// The bytes as they are, no length.
    /// </summary>
    public void WriteBytes(scoped ReadOnlySpan<byte> value)
    {
        var span = Take(value.Length);

        if (span.Length == value.Length)
        {
            value.CopyTo(span);
        }
    }

#endregion Primitives

#region Arrays

    /// <summary>
    /// The count of a list in the prefix the client reads. A list longer than the prefix can count is a bug of the caller
    /// and throws, as the <c>checked</c> casts of the hand-written writers did.
    /// </summary>
    public void WriteLength(WireLength prefix, int count)
    {
        switch (prefix)
        {
            case WireLength.Byte:
                WriteByte(checked((byte)count));
                break;

            case WireLength.Signed16:
                WriteInt16(checked((short)count));
                break;

            case WireLength.Unsigned16:
                WriteUInt16(checked((ushort)count));
                break;

            case WireLength.Signed32:
                ArgumentOutOfRangeException.ThrowIfNegative(count);
                WriteInt32(count);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(prefix), prefix, "Not a prefix of a list.");
        }
    }

    /// <summary>
    /// The values one after another, no length.
    /// </summary>
    public void WriteInt32Array(scoped ReadOnlySpan<int> values)
    {
        foreach (var value in values)
        {
            WriteInt32(value);
        }
    }

    /// <inheritdoc cref="WriteInt32Array"/>
    public void WriteInt16Array(scoped ReadOnlySpan<short> values)
    {
        foreach (var value in values)
        {
            WriteInt16(value);
        }
    }

    /// <inheritdoc cref="WriteInt32Array"/>
    public void WriteSingleArray(scoped ReadOnlySpan<float> values)
    {
        foreach (var value in values)
        {
            WriteSingle(value);
        }
    }

    /// <inheritdoc cref="WriteInt32Array"/>
    public void WriteBooleanArray(scoped ReadOnlySpan<bool> values)
    {
        foreach (var value in values)
        {
            WriteBoolean(value);
        }
    }

#endregion Arrays

#region Strings

    // The formats are the ones of the helpers of BinaryWriter of the same names, quirks included: the "Utf8" and "Utf16"
    // of a name say how the length is counted, the "Ascii" and "Unicode" how the characters are encoded (one byte, or two
    // little endian), and a terminator is one zero byte even after two-byte characters. A missing string is written as an
    // empty one.

    /// <summary>
    /// The characters in ASCII, then one zero byte; no length.
    /// </summary>
    public void WriteAsciiString(string? value)
    {
        WriteAscii(value);
        WriteByte(0);
    }

    /// <summary>
    /// The characters in ASCII; no length, no terminator.
    /// </summary>
    public void WriteAsciiStringWithoutTerminator(string? value)
    {
        WriteAscii(value);
    }

    /// <summary>
    /// A 16-bit length, the characters plus one; the characters in ASCII; one zero byte.
    /// </summary>
    public void WriteUtf8AsciiString(string? value)
    {
        value ??= string.Empty;

        WriteInt16((short)(value.Length + 1));
        WriteAsciiString(value);
    }

    /// <summary>
    /// A 16-bit length, twice the characters plus one; the characters in ASCII; one zero byte.
    /// </summary>
    public void WriteUtf16AsciiString(string? value)
    {
        value ??= string.Empty;

        WriteInt16((short)(value.Length * 2 + 1));
        WriteAsciiString(value);
    }

    /// <summary>
    /// A 16-bit length, the characters; the characters in ASCII.
    /// </summary>
    public void WriteUtf8AsciiStringWithoutTerminator(string? value)
    {
        value ??= string.Empty;

        WriteInt16((short)value.Length);
        WriteAscii(value);
    }

    /// <summary>
    /// A 16-bit length, the characters plus one; the characters in UTF-16; one zero byte.
    /// </summary>
    public void WriteUtf8UnicodeString(string? value)
    {
        value ??= string.Empty;

        WriteInt16((short)(value.Length + 1));
        WriteUnicode(value);
        WriteByte(0);
    }

    /// <summary>
    /// A 16-bit length, twice the characters plus one; the characters in UTF-16; one zero byte.
    /// </summary>
    public void WriteUtf16UnicodeString(string? value)
    {
        value ??= string.Empty;

        WriteInt16((short)(2 * value.Length + 1));
        WriteUnicode(value);
        WriteByte(0);
    }

    /// <summary>
    /// A 16-bit length, the characters; the characters in UTF-16.
    /// </summary>
    public void WriteUtf8UnicodeStringWithoutTerminator(string? value)
    {
        value ??= string.Empty;

        WriteInt16((short)value.Length);
        WriteUnicode(value);
    }

    /// <summary>
    /// A 16-bit count of characters, then the characters in UTF-16: the layout <c>ReadUtf16UnicodeStringWithoutTerminator</c>
    /// reads, the same bytes as <see cref="WriteUtf8UnicodeStringWithoutTerminator"/>.
    /// </summary>
    public void WriteUtf16UnicodeStringWithoutTerminator(string? value)
    {
        WriteUtf8UnicodeStringWithoutTerminator(value);
    }

    private void WriteAscii(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        // A character outside ASCII becomes one '?', a pair of surrogates one '?' too: count, do not assume.
        var count = Encoding.ASCII.GetByteCount(value);
        var span = Take(count);

        if (span.Length == count)
        {
            Encoding.ASCII.GetBytes(value, span);
        }
    }

    private void WriteUnicode(string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        var count = Encoding.Unicode.GetByteCount(value);
        var span = Take(count);

        if (span.Length == count)
        {
            Encoding.Unicode.GetBytes(value, span);
        }
    }

#endregion Strings

#region Room

    /// <summary>
    /// The next <paramref name="count"/> bytes to write; an empty span when they do not fit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> Take(int count)
    {
        var position = _position;

        if ((uint)count <= (uint)(_span.Length - position))
        {
            _position = position + count;

            return _span.Slice(position, count);
        }

        return TakeLarger(count);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Span<byte> TakeLarger(int count)
    {
        var end = (long)_position + count;

        if (_overflowed || count < 0 || end > _limit || _array is null)
        {
            Overflow();

            return default;
        }

        var size = (int)Math.Max(end, Math.Min(_limit, 2L * _span.Length));
        var larger = _pool!.Rent(size);

        _span[.._position].CopyTo(larger);
        _pool.Return(_array);

        _array = larger;
        _span = Usable(larger, _limit);

        var position = _position;

        _position = (int)end;

        return _span.Slice(position, count);
    }

    /// <summary>
    /// A pool may hand out more than was asked for: what lies past the limit is not room.
    /// </summary>
    private static Span<byte> Usable(byte[] array, int limit)
    {
        return array.AsSpan(0, Math.Min(array.Length, limit));
    }

    private void Overflow()
    {
        _overflowed = true;

        // Nothing fits after the first write that did not: the message stays cut where it was cut.
        _span = _span[.._position];
    }

#endregion Room
}
