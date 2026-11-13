using System.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot;

/// <summary>
/// Turns a message into a <see cref="Frame"/>: room for the header, the opcode, the body, the body encrypted in place,
/// then the header, all in the array the frame keeps, with no second copy. Every message writes itself through
/// <see cref="PacketWriter"/> (<see cref="IWireWritable"/>, generated for a <c>[WireMessage]</c>) straight into the array of
/// the frame. The writer is a local over an array of the pool, so the encoder holds no state of a message, and a message
/// written while another one is needs no encoder of its own. The world loop has its own encoder
/// (<see cref="IEncoderThread"/>), which reports what it drops; any other thread gets one that reports to nobody.<br/>
/// A body that does not fit one frame stops the writer at the limit; the message is dropped with a warning and counted in
/// <c>frames.dropped</c>, and nothing throws on the loop.
/// </summary>
internal sealed partial class FrameEncoder
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
    private readonly IFrameDrops? _metrics;
    private readonly ArrayPool<byte> _arrays;

    /// <param name="logger">Where a dropped message is reported.</param>
    /// <param name="metrics">Counts the dropped messages; <see langword="null"/> for an encoder nobody measures.</param>
    /// <param name="arrays">Where the arrays of the frames come from; the shared pool unless a test watches them.</param>
    public FrameEncoder(ILogger logger, IFrameDrops? metrics = null, ArrayPool<byte>? arrays = null)
    {
        _logger = logger;
        _metrics = metrics;
        _arrays = arrays ?? ArrayPool<byte>.Shared;
    }

    /// <summary>
    /// The encoder of the world loop on the loop thread, and one per thread elsewhere (it reports to nobody).
    /// </summary>
    public static FrameEncoder Current => SynchronizationContext.Current is IEncoderThread { IsCurrent: true } loop
        ? loop.Encoder
        : t_encoder ??= new FrameEncoder(NullLogger.Instance);

    /// <summary>
    /// A message given as its struct is called where it is: nothing is boxed, nothing allocated but the frame of the pools.
    /// </summary>
    /// <returns>The frame with one reference, the caller's; <see langword="null"/> when the body does not fit.</returns>
    public Frame? Encode<T>(in T message) where T : IResponseHotspotMessage
    {
        var writer = new PacketWriter(_arrays, InitialCapacity, MaxFrameLength, MessageHeader.HeaderSize);

        try
        {
            writer.WriteInt16(message.Opcode.Value);

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
    /// Encrypts the body in place and writes the header in front of it.
    /// </summary>
    private Frame Seal(byte[] buffer, int length)
    {
        var header = new MessageHeader(length - MessageHeader.HeaderSize);

        MessageEncryption.Exchange(buffer.AsSpan(MessageHeader.HeaderSize, header.ContentLength), header.Version);

        header.Write(buffer);

        return Frame.Create(buffer, length, _arrays);
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
}
