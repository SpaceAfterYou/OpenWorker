using System.Text;

namespace OpenWorker.Hotspot.Handler;

/// <summary>
/// A <see cref="BinaryReader"/> over a stream whose bytes are set per packet, so that reading a request makes neither a
/// stream nor a reader. <see cref="HandlerExecutor"/> keeps the ones no packet uses.
/// </summary>
internal sealed class RequestReader : IDisposable
{
    public RequestReader()
    {
        Binary = new BinaryReader(Stream, Encoding.UTF8, leaveOpen: true);
    }

    public PacketStream Stream { get; } = new();

    public BinaryReader Binary { get; }

    public void Dispose()
    {
        Binary.Dispose();
        Stream.Dispose();
    }

    /// <summary>
    /// A stream that reads the bytes of one packet; setting <see cref="Source"/> starts it over on another.
    /// </summary>
    internal sealed class PacketStream : Stream
    {
        private ReadOnlyMemory<byte> _source;
        private int _position;

        public ReadOnlyMemory<byte> Source
        {
            get => _source;
            set
            {
                _source = value;
                _position = 0;
            }
        }

        public override int Read(Span<byte> buffer)
        {
            var count = Math.Min(buffer.Length, _source.Length - _position);

            _source.Span.Slice(_position, count).CopyTo(buffer);

            _position += count;

            return count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override int ReadByte()
        {
            return _position < _source.Length ? _source.Span[_position++] : -1;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _source.Length;

        public override long Position
        {
            get => _position;
            set => _position = (int)Math.Clamp(value, 0, _source.Length);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _source.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            return _position;
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
