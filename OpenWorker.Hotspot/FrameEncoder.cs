using System.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Hotspot.Telemetry;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot;

/// <summary>
/// Turns a message into a <see cref="Frame"/>: room for the header, the opcode, the body, the body encrypted in place,
/// then the header, all in the array the frame keeps, with no second copy. The writer and the stream under it are
/// made once and reused for every message, so an encoder belongs to one thread: the world loop has its own
/// (<see cref="WorldLoop"/>), and any other thread that sends gets one of its own.<br/>
/// Two paths live side by side until every message has moved: a message that writes itself through
/// <see cref="PacketWriter"/> (<see cref="IWireWritable"/>, generated for a <c>[WireMessage]</c>) is written straight into
/// the array of the frame; any other goes through the <see cref="BinaryWriter"/> over <see cref="FrameStream"/>.<br/>
/// A body that does not fit one frame stops the writer or the stream at the limit; the message is dropped with a warning
/// and counted in <c>frames.dropped</c>, and nothing throws on the loop.
/// </summary>
internal sealed partial class FrameEncoder : IDisposable
{
    /// <summary>
    /// What a frame starts with. A movement takes a quarter of it; a longer message grows the array as it is written.
    /// </summary>
    internal const int InitialCapacity = 256;

    /// <summary>
    /// The largest frame: the 16-bit length of the header counts the header too.
    /// </summary>
    private const int MaxFrameLength = MessageHeader.HeaderSize + MessageHeader.MaxContentLength;

    [ThreadStatic]
    private static FrameEncoder? t_encoder;

    private readonly ILogger _logger;
    private readonly SessionMetrics? _metrics;
    private readonly ArrayPool<byte> _arrays;
    private readonly FrameStream _stream;
    private readonly BinaryWriter _writer;

    private bool _busy;

    /// <param name="logger">Where a dropped message is reported.</param>
    /// <param name="metrics">Counts the dropped messages; <see langword="null"/> for an encoder nobody measures.</param>
    /// <param name="arrays">Where the arrays of the frames come from; the shared pool unless a test watches them.</param>
    public FrameEncoder(ILogger logger, SessionMetrics? metrics = null, ArrayPool<byte>? arrays = null)
    {
        _logger = logger;
        _metrics = metrics;
        _arrays = arrays ?? ArrayPool<byte>.Shared;
        _stream = new FrameStream(_arrays);
        _writer = new BinaryWriter(_stream);
    }

    /// <summary>
    /// The encoder of the world loop on the loop thread, and one per thread elsewhere (it reports to nobody).
    /// </summary>
    public static FrameEncoder Current => SynchronizationContext.Current is WorldLoop { IsCurrent: true } loop
        ? loop.Encoder
        : t_encoder ??= new FrameEncoder(NullLogger.Instance);

    /// <returns>The frame with one reference, the caller's; <see langword="null"/> when the body does not fit.</returns>
    public Frame? Encode(IResponseHotspotMessage message)
    {
        if (message is IWireWritable wire)
        {
            return Encode(message, wire);
        }

        // A message that sends another one while it is written: rare enough to pay for a writer of its own.
        if (_busy)
        {
            using var nested = new FrameEncoder(_logger, _metrics, _arrays);

            return nested.Encode(message);
        }

        _busy = true;

        try
        {
            _stream.Begin();

            _writer.Write(message.Opcode);

            message.Write(_writer);

            _writer.Flush();
        }

        catch
        {
            _stream.Abandon();

            throw;
        }

        finally
        {
            _busy = false;
        }

        if (_stream.Overflowed)
        {
            _stream.Abandon();

            OnDropped(message.Opcode);

            return null;
        }

        var buffer = _stream.Detach(out var length);

        return Seal(buffer, length);
    }

    /// <summary>
    /// A message with a span format, called on its struct: nothing is boxed, nothing allocated but the frame of the pools.
    /// </summary>
    public Frame? Encode<T>(in T message) where T : struct, IResponseHotspotMessage, IWireWritable
    {
        var writer = new PacketWriter(_arrays, InitialCapacity, MaxFrameLength, MessageHeader.HeaderSize);

        try
        {
            writer.WriteInt16(message.Opcode);

            message.Write(ref writer);
        }

        catch
        {
            writer.Dispose();

            throw;
        }

        if (writer.Overflowed)
        {
            writer.Dispose();

            OnDropped(message.Opcode);

            return null;
        }

        var buffer = writer.Detach(out var length);

        return Seal(buffer, length);
    }

    /// <summary>
    /// The path of a message with a span format: the writer is a local over an array of the pool, so the encoder holds no
    /// state of it and a message written while another one is needs no encoder of its own.
    /// </summary>
    private Frame? Encode(IResponseHotspotMessage message, IWireWritable wire)
    {
        var writer = new PacketWriter(_arrays, InitialCapacity, MaxFrameLength, MessageHeader.HeaderSize);

        try
        {
            writer.WriteInt16(message.Opcode);

            wire.Write(ref writer);
        }

        catch
        {
            writer.Dispose();

            throw;
        }

        if (writer.Overflowed)
        {
            writer.Dispose();

            OnDropped(message.Opcode);

            return null;
        }

        var buffer = writer.Detach(out var length);

        return Seal(buffer, length);
    }

    /// <summary>
    /// Encrypts the body in place and writes the header in front of it.
    /// </summary>
    private Frame Seal(byte[] buffer, int length)
    {
        var header = new MessageHeader(length - MessageHeader.HeaderSize);

        MessageEncryption.Exchange(buffer.AsSpan(MessageHeader.HeaderSize, header.ContentLength), header.Version);

        header.Write(buffer);

        return Frame.Create(buffer, length, _arrays);
    }

    /// <summary>
    /// Lets the writer and its stream go. The encoder of a thread other than the loop lives as long as its thread.
    /// </summary>
    public void Dispose()
    {
        _writer.Dispose();
    }

    private void OnDropped(MessageOpcode message)
    {
        // Only a message that is dropped pays for the name of its opcode.
        var opcode = message.ToFullString();

        _metrics?.Dropped(opcode);

        LogDropped(_logger, opcode, MessageHeader.MaxContentLength);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dropped the message {Opcode}: its body is longer than the {Limit} bytes of one frame.")]
    private static partial void LogDropped(ILogger logger, string opcode, int limit);

    /// <summary>
    /// A stream that writes into an array of the pool, from just after the header, and grows it by renting a larger one.
    /// It stops at the largest frame instead of throwing: what is written past it is dropped and <see cref="Overflowed"/>
    /// says so.
    /// </summary>
    private sealed class FrameStream(ArrayPool<byte> arrays) : Stream
    {
        private byte[] _buffer = [];
        private int _position;

        public bool Overflowed { get; private set; }

        public void Begin()
        {
            _buffer = arrays.Rent(InitialCapacity);
            _position = MessageHeader.HeaderSize;

            Overflowed = false;
        }

        /// <summary>
        /// Hands the array over to the frame.
        /// </summary>
        public byte[] Detach(out int length)
        {
            var buffer = _buffer;

            length = _position;

            _buffer = [];
            _position = 0;

            return buffer;
        }

        public void Abandon()
        {
            if (_buffer.Length > 0)
            {
                arrays.Return(_buffer);
            }

            _buffer = [];
            _position = 0;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            var end = _position + buffer.Length;

            if (Overflowed || !Reserve(end))
            {
                return;
            }

            buffer.CopyTo(_buffer.AsSpan(_position));

            _position = end;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer.AsSpan(offset, count));
        }

        public override void WriteByte(byte value)
        {
            if (Overflowed || !Reserve(_position + 1))
            {
                return;
            }

            _buffer[_position++] = value;
        }

        /// <summary>
        /// Makes room up to <paramref name="end"/>, or marks the frame overflowed past the largest one.
        /// </summary>
        private bool Reserve(int end)
        {
            if (end <= _buffer.Length)
            {
                return true;
            }

            if (end > MaxFrameLength)
            {
                Overflowed = true;

                return false;
            }

            var larger = arrays.Rent(Math.Max(end, Math.Min(MaxFrameLength, _buffer.Length * 2)));

            _buffer.AsSpan(0, _position).CopyTo(larger);

            arrays.Return(_buffer);

            _buffer = larger;

            return true;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _position;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
