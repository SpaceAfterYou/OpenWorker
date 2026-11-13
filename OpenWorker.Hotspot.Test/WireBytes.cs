using System.Buffers;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The bytes a message or a value writes through <see cref="PacketWriter"/>, the way the frame encoder writes it: what a
/// frame carries after the opcode.
/// </summary>
internal static class WireBytes
{
    public static byte[] Of<T>(in T value) where T : IWireWritable
    {
        var writer = new PacketWriter(ArrayPool<byte>.Shared, 256, Array.MaxLength);

        try
        {
            value.Write(ref writer);

            Assert.IsFalse(writer.Overflowed, $"{typeof(T).Name} does not fit an array.");

            return writer.Written.ToArray();
        }

        finally
        {
            writer.Dispose();
        }
    }
}
