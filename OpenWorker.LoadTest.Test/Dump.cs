using System.Buffers.Binary;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.LoadTest.Test;

/// <summary>
/// A packet a real client sent, saved by the servers in <c>OpenWorker.Hotspot.Test/Datas</c>: the header as it
/// came, and the body decrypted, with the passwords and tickets blacked out.
/// </summary>
internal sealed record Dump(ushort Version, byte Direction, byte[] Body)
{
    private const string Folder = "Datas/1.4.2.3";

    /// <summary>
    /// The frame as it was on the wire: the header and the body encrypted again with the version of the header.
    /// </summary>
    public byte[] WireFrame()
    {
        var wire = new byte[MessageHeader.HeaderSize + Body.Length];

        // The header of the client carries its own version, and the dump keeps it.
        BinaryPrimitives.WriteUInt16LittleEndian(wire, Version);
        BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(2), (ushort)wire.Length);
        wire[4] = Direction;

        Body.CopyTo(wire, MessageHeader.HeaderSize);

        MessageEncryption.Exchange(wire.AsSpan(MessageHeader.HeaderSize), Version);

        return wire;
    }

    /// <summary>
    /// The reader of the server's kind, after the opcode.
    /// </summary>
    public BinaryReader Reader()
    {
        return new BinaryReader(new MemoryStream(Body) { Position = sizeof(ushort) });
    }

    public static Dump Read(byte group, byte command)
    {
        var path = Path.Combine(AppContext.BaseDirectory, Folder, $"0x{group:X2},0x{command:X2}.bin");

        Assert.IsTrue(File.Exists(path), $"The dump {path} is missing.");

        var bytes = File.ReadAllBytes(path);
        var header = new MessageHeader(bytes);

        Assert.AreEqual(MessageHeader.HeaderSize + header.ContentLength, bytes.Length, "The dump is cut.");

        return new Dump(header.Version, bytes[4], bytes[MessageHeader.HeaderSize..]);
    }
}
