using System.Buffers.Binary;
using System.Text;

namespace OpenWorker.LoadTest.Protocol;

/// <summary>
/// Reads the body of a frame the server sent, with no stream and no reader object. A read past the end of the
/// body throws <see cref="InvalidDataException" />: the bot treats a packet it cannot read as a fault of the
/// protocol and stops, rather than going on with half a message.
/// </summary>
internal ref struct PacketReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    public PacketReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    /// <summary>
    /// A reader at the first byte after the opcode, which is how the bodies of the messages begin.
    /// </summary>
    public static PacketReader AfterOpcode(ReadOnlySpan<byte> frameBody)
    {
        if (frameBody.Length < sizeof(ushort))
        {
            throw new InvalidDataException("The frame is shorter than its opcode.");
        }

        return new PacketReader(frameBody) { _position = sizeof(ushort) };
    }

    public readonly int Position => _position;

    public readonly int Remaining => _data.Length - _position;

    public byte ReadByte()
    {
        Require(sizeof(byte));

        return _data[_position++];
    }

    public bool ReadBool() => ReadByte() != 0;

    public short ReadInt16()
    {
        Require(sizeof(short));

        var value = BinaryPrimitives.ReadInt16LittleEndian(_data[_position..]);
        _position += sizeof(short);

        return value;
    }

    public ushort ReadUInt16() => (ushort)ReadInt16();

    public int ReadInt32()
    {
        Require(sizeof(int));

        var value = BinaryPrimitives.ReadInt32LittleEndian(_data[_position..]);
        _position += sizeof(int);

        return value;
    }

    public long ReadInt64()
    {
        Require(sizeof(long));

        var value = BinaryPrimitives.ReadInt64LittleEndian(_data[_position..]);
        _position += sizeof(long);

        return value;
    }

    public ulong ReadUInt64() => (ulong)ReadInt64();

    public float ReadSingle()
    {
        Require(sizeof(float));

        var value = BinaryPrimitives.ReadSingleLittleEndian(_data[_position..]);
        _position += sizeof(float);

        return value;
    }

    public void Skip(int count)
    {
        Require(count);

        _position += count;
    }

    /// <summary>
    /// ASCII up to a zero byte, which is read and dropped.
    /// </summary>
    public string ReadAsciiTerminated()
    {
        var rest = _data[_position..];
        var end = rest.IndexOf((byte)0);

        if (end < 0)
        {
            throw new InvalidDataException("A string has no terminator before the end of the packet.");
        }

        _position += end + 1;

        return Encoding.ASCII.GetString(rest[..end]);
    }

    /// <summary>
    /// ASCII with its length in front, the length counting the zero byte at the end.
    /// </summary>
    public string ReadAsciiWithTerminator()
    {
        var length = ReadLength(1);

        return Encoding.ASCII.GetString(Take(length)[..^1]);
    }

    /// <summary>
    /// ASCII with its length in front and no zero byte at the end.
    /// </summary>
    public string ReadAscii()
    {
        var length = ReadLength(0);

        return Encoding.ASCII.GetString(Take(length));
    }

    /// <summary>
    /// UTF-16 with the number of bytes in front, counting a zero byte at the end.
    /// </summary>
    public string ReadUnicode()
    {
        var length = ReadLength(1);

        return Encoding.Unicode.GetString(Take(length)[..^1]);
    }

    private int ReadLength(int minimum)
    {
        var length = ReadInt16();

        if (length < minimum)
        {
            throw new InvalidDataException($"A string of {length} bytes cannot hold its terminator.");
        }

        return length;
    }

    private ReadOnlySpan<byte> Take(int count)
    {
        Require(count);

        var span = _data.Slice(_position, count);
        _position += count;

        return span;
    }

    private readonly void Require(int count)
    {
        if (count < 0 || count > _data.Length - _position)
        {
            throw new InvalidDataException($"The packet is cut short: {count} bytes wanted at {_position} of {_data.Length}.");
        }
    }
}
