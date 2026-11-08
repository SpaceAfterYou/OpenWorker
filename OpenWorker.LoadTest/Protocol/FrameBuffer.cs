using System.Buffers;
using System.Buffers.Binary;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.LoadTest.Protocol;

/// <summary>
/// The bytes a connection has received and not yet used, which the read loop of the connection refills and
/// empties: the socket is read into the free end, and the whole frames at the front are handed out, decrypted in
/// place. One pooled buffer serves the connection for its whole life; it grows only for a frame that is bigger
/// than it, and goes back to the pool on <see cref="Dispose" />.
/// </summary>
internal sealed class FrameBuffer : IDisposable
{
    private const int InitialSize = 4096;

    /// <summary>
    /// The room that is left for a read: with less than this at the end, the data moves to the front first.
    /// </summary>
    private const int MinimumRead = 1024;

    private byte[] _buffer = ArrayPool<byte>.Shared.Rent(InitialSize);
    private int _start;
    private int _end;

    /// <summary>
    /// The size of the frame that is waiting for its last bytes, so the buffer is made big enough for it.
    /// </summary>
    private int _wanted;

    public int Capacity => _buffer.Length;

    public int Buffered => _end - _start;

    /// <summary>
    /// Where the next read of the socket goes.
    /// </summary>
    public Memory<byte> ReceiveMemory()
    {
        if (_start == _end)
        {
            _start = 0;
            _end = 0;
        }

        var needed = Math.Max(_wanted, Buffered + MinimumRead);

        if (_buffer.Length - _start < needed)
        {
            if (_buffer.Length >= needed)
            {
                Compact();
            }

            else
            {
                Grow(needed);
            }
        }

        return _buffer.AsMemory(_end);
    }

    /// <summary>
    /// Tells that the read put this many bytes at the end.
    /// </summary>
    public void Advance(int count)
    {
        _end += count;
    }

    /// <summary>
    /// Takes the first whole frame, decrypts its body and gives it, opcode first. False when the bytes of the
    /// next frame have not all come.
    /// </summary>
    public bool TryReadFrame(out Span<byte> body)
    {
        body = default;

        if (Buffered < MessageHeader.HeaderSize)
        {
            _wanted = MessageHeader.HeaderSize;

            return false;
        }

        var head = _buffer.AsSpan(_start, MessageHeader.HeaderSize);
        var version = BinaryPrimitives.ReadUInt16LittleEndian(head);
        var length = BinaryPrimitives.ReadUInt16LittleEndian(head[2..]);

        if (length < MessageHeader.HeaderSize)
        {
            throw new InvalidDataException($"The frame says it is {length} bytes, less than its header.");
        }

        if (!FrameCipher.IsKnown(version))
        {
            throw new InvalidDataException($"The frame has the version {version}, and the key table has no keys for it.");
        }

        if (Buffered < length)
        {
            _wanted = length;

            return false;
        }

        _wanted = 0;

        body = _buffer.AsSpan(_start + MessageHeader.HeaderSize, length - MessageHeader.HeaderSize);

        FrameCipher.Apply(body, version);

        _start += length;

        return true;
    }

    public void Dispose()
    {
        var buffer = Interlocked.Exchange(ref _buffer, []);

        if (buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void Compact()
    {
        Buffer.BlockCopy(_buffer, _start, _buffer, 0, Buffered);

        _end -= _start;
        _start = 0;
    }

    private void Grow(int needed)
    {
        var bigger = ArrayPool<byte>.Shared.Rent(Math.Max(needed, _buffer.Length * 2));

        Buffer.BlockCopy(_buffer, _start, bigger, 0, Buffered);

        ArrayPool<byte>.Shared.Return(_buffer);

        _buffer = bigger;
        _end -= _start;
        _start = 0;
    }
}
