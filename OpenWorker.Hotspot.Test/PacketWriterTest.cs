using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class PacketWriterTest
{
    private static readonly byte[] FirstValue = [0x01, 0x02, 0x03, 0x04];

    /// <summary>
    /// A write that does not fit does not throw: the writer records the overflow and keeps the message cut where it was
    /// cut, even when a later, smaller write would fit, so a caller that checks <see cref="PacketWriter.Overflowed"/> once at
    /// the end never sends a message with a hole in it.
    /// </summary>
    [TestMethod]
    public void AWritePastTheLimitIsRecordedAndNothingAfterItIsWritten()
    {
        Span<byte> buffer = stackalloc byte[6];

        var writer = new PacketWriter(buffer);

        writer.WriteInt32(0x04030201);
        writer.WriteInt32(0x08070605);
        writer.WriteByte(0x09);

        Assert.IsTrue(writer.Overflowed);
        Assert.AreEqual(4, writer.Position);
        CollectionAssert.AreEqual(FirstValue, writer.Written.ToArray());

        // Over an array of a pool the writer grows, up to its limit and no further.
        var pooled = new PacketWriter(System.Buffers.ArrayPool<byte>.Shared, capacity: 8, limit: 64, start: 5);

        try
        {
            pooled.WriteUtf16UnicodeString(new string('a', 20));

            Assert.IsFalse(pooled.Overflowed, "5 + 2 + 40 + 1 bytes fit the limit.");
            Assert.AreEqual(48, pooled.Position);

            pooled.WriteBytes(new byte[17]);

            Assert.IsTrue(pooled.Overflowed);
            Assert.AreEqual(48, pooled.Position);
        }

        finally
        {
            pooled.Dispose();
        }
    }
}
