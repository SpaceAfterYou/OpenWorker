using System.Buffers.Binary;
using System.Text;

namespace OpenWorker.Content.Tool.Vision;

/// <summary>
/// Little-endian reader over a Vision binary blob.
/// </summary>
internal sealed class BinaryCursor
{
    private readonly byte[] _data;
    private int _position;

    public BinaryCursor(byte[] data)
    {
        _data = data;
    }

    public int Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > _data.Length)
            {
                throw new InvalidDataException($"Seek out of range: {value}.");
            }

            _position = value;
        }
    }

    public int Remaining => _data.Length - _position;

    public bool Eof => _position >= _data.Length;

    public void Seek(int position) => Position = position;

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        if (count < 0 || _position + count > _data.Length)
        {
            throw new InvalidDataException($"Unexpected end of data at {_position}, need {count}.");
        }

        var span = _data.AsSpan(_position, count);
        _position += count;
        return span;
    }

    public byte ReadUInt8() => ReadBytes(1)[0];

    public sbyte ReadInt8() => unchecked((sbyte)ReadUInt8());

    public bool ReadBool() => ReadUInt8() != 0;

    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2));

    public short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(2));

    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(4));

    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));

    public long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(ReadBytes(8));

    public ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(ReadBytes(8));

    public float ReadFloat() => BinaryPrimitives.ReadSingleLittleEndian(ReadBytes(4));

    public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(ReadBytes(8));

    public int ReadChunkId() => ReadInt32();

    public string ReadUtf8UInt32()
    {
        var length = checked((int)ReadUInt32());
        if (length <= 0)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(ReadBytes(length));
    }

}
