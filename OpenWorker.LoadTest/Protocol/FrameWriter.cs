using System.Buffers.Binary;
using System.Text;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.LoadTest.Protocol;

/// <summary>
/// Writes the frames of one request or more into the writer it is given.
/// </summary>
internal delegate void Encoder(ref FrameWriter writer);

/// <summary>
/// Builds the frames a client sends into a buffer, with no stream and no writer object. A frame is the header of
/// five bytes (version, length with the header, direction) and the encrypted body, which opens with the opcode.
/// <see cref="Begin" /> and <see cref="End" /> bracket one frame, and several frames can follow each other in the
/// same buffer to go out in one write.
/// </summary>
internal ref struct FrameWriter
{
    /// <summary>
    /// What a real client puts in the version of the header. The server decrypts with the version it reads, so any
    /// known version works; the packet dumps of the client all carry this one.
    /// </summary>
    public const ushort ClientVersion = 0;

    private const byte DirectionGame = 1;

    private readonly Span<byte> _buffer;
    private int _frameStart;
    private int _position;

    public FrameWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _frameStart = 0;
        _position = 0;
    }

    /// <summary>
    /// The bytes written so far, which are the frames closed with <see cref="End" />.
    /// </summary>
    public readonly int Length => _position;

    public void Begin(ushort opcode)
    {
        _frameStart = _position;
        _position += MessageHeader.HeaderSize;

        WriteUInt16(opcode);
    }

    /// <summary>
    /// Writes the header of the frame and encrypts its body.
    /// </summary>
    public void End()
    {
        var length = _position - _frameStart;

        if (length > ushort.MaxValue)
        {
            throw new InvalidOperationException($"A frame of {length} bytes does not fit the length field.");
        }

        var header = _buffer.Slice(_frameStart, MessageHeader.HeaderSize);

        BinaryPrimitives.WriteUInt16LittleEndian(header, ClientVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(header[2..], (ushort)length);
        header[4] = DirectionGame;

        FrameCipher.Apply(_buffer[(_frameStart + MessageHeader.HeaderSize).._position], ClientVersion);
    }

    public void WriteByte(byte value)
    {
        _buffer[_position++] = value;
    }

    public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    public void WriteInt16(short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(_buffer[_position..], value);
        _position += sizeof(short);
    }

    public void WriteUInt16(ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer[_position..], value);
        _position += sizeof(ushort);
    }

    public void WriteInt32(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(_buffer[_position..], value);
        _position += sizeof(int);
    }

    public void WriteInt64(long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(_buffer[_position..], value);
        _position += sizeof(long);
    }

    public void WriteUInt64(ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer[_position..], value);
        _position += sizeof(ulong);
    }

    public void WriteSingle(float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(_buffer[_position..], value);
        _position += sizeof(float);
    }

    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        value.CopyTo(_buffer[_position..]);
        _position += value.Length;
    }

    /// <summary>
    /// A string the way the client writes it in a request: the number of bytes with the terminator (the text in
    /// UTF-16, then one zero byte) as 16 bits, and then the text and the terminator.
    /// </summary>
    public void WriteString(string value)
    {
        var bytes = Encoding.Unicode.GetByteCount(value);

        if (bytes + 1 > short.MaxValue)
        {
            throw new ArgumentException("The string is too long for its length field.", nameof(value));
        }

        WriteInt16((short)(bytes + 1));

        _position += Encoding.Unicode.GetBytes(value, _buffer[_position..]);

        WriteByte(0);
    }
}
