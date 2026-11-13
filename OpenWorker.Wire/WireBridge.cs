using System.Buffers;

namespace OpenWorker.Wire;

/// <summary>
/// Carries a value with a span format over to code that still reads through <see cref="BinaryReader"/> or writes through
/// <see cref="BinaryWriter"/>: the tests and the bot of the load test. The generator calls it from the constructor that
/// takes a <see cref="BinaryReader"/>, so both readers give the same value. Not for the hot path: the frame encoder and the
/// handler executor use the spans directly.
/// </summary>
public static class WireBridge
{
    private const int InitialCapacity = 256;

    /// <summary>
    /// Reads a <typeparamref name="T"/> at the position of the stream of <paramref name="reader"/> and moves the stream
    /// past it. The stream must be seekable, as every stream of a packet is.
    /// </summary>
    public static T Read<T>(BinaryReader reader) where T : IWireReadable<T>
    {
        ArgumentNullException.ThrowIfNull(reader);

        var stream = reader.BaseStream;

        if (!stream.CanSeek)
        {
            throw new NotSupportedException("A packet is read from a stream that can seek.");
        }

        var start = stream.Position;
        var left = checked((int)(stream.Length - start));

        if (stream is MemoryStream memory && memory.TryGetBuffer(out var segment))
        {
            var packet = new PacketReader(segment.AsSpan(checked((int)start), left));
            var value = T.Read(ref packet);

            stream.Position = start + packet.Position;

            return value;
        }

        var rented = ArrayPool<byte>.Shared.Rent(left);

        try
        {
            stream.ReadExactly(rented, 0, left);

            var packet = new PacketReader(rented.AsSpan(0, left));
            var value = T.Read(ref packet);

            stream.Position = start + packet.Position;

            return value;
        }

        catch
        {
            stream.Position = start;

            throw;
        }

        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Writes <paramref name="value"/> through <see cref="PacketWriter"/> and hands the bytes to <paramref name="writer"/>.
    /// </summary>
    public static void Write<T>(BinaryWriter writer, in T value) where T : IWireWritable
    {
        ArgumentNullException.ThrowIfNull(writer);

        var packet = new PacketWriter(ArrayPool<byte>.Shared, InitialCapacity, Array.MaxLength);

        try
        {
            value.Write(ref packet);

            if (packet.Overflowed)
            {
                throw new InvalidOperationException($"{typeof(T).Name} does not fit an array.");
            }

            writer.Write(packet.Written);
        }

        finally
        {
            packet.Dispose();
        }
    }
}
