using System.Buffers.Binary;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.LoadTest.Protocol;

namespace OpenWorker.LoadTest.Test;

/// <summary>
/// One frame taken apart with the types of the server: the header, and the body decrypted with the server's own
/// encryption. The bot's codec is checked against this, never against itself.
/// </summary>
internal sealed record Frame(ushort Version, int Length, byte Direction, byte[] Body)
{
    public ushort Opcode => BinaryPrimitives.ReadUInt16LittleEndian(Body);

    /// <summary>
    /// A reader of the server's kind, at the first byte after the opcode: the request types of the server
    /// are built from it.
    /// </summary>
    public BinaryReader Reader()
    {
        var stream = new MemoryStream(Body) { Position = sizeof(ushort) };

        return new BinaryReader(stream);
    }
}

internal static class Wire
{
    public static byte[] Encode(Encoder encode)
    {
        var buffer = new byte[Requests.MaxFrameSize * 2];
        var writer = new FrameWriter(buffer);

        encode(ref writer);

        return buffer.AsSpan(0, writer.Length).ToArray();
    }

    /// <summary>
    /// Splits one frame, which must be the whole of the bytes.
    /// </summary>
    public static Frame Split(ReadOnlySpan<byte> bytes)
    {
        var header = new MessageHeader(bytes);

        Assert.AreEqual(MessageHeader.HeaderSize + header.ContentLength, bytes.Length, "The length in the header and the bytes differ.");

        var body = bytes[MessageHeader.HeaderSize..].ToArray();

        MessageEncryption.Exchange(body.AsSpan(), header.Version);

        return new Frame(header.Version, bytes.Length, bytes[4], body);
    }

    /// <summary>
    /// The frames of a buffer that holds several, one after the other.
    /// </summary>
    public static List<Frame> SplitAll(ReadOnlySpan<byte> bytes)
    {
        var frames = new List<Frame>();

        while (!bytes.IsEmpty)
        {
            var length = BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]);

            frames.Add(Split(bytes[..length]));

            bytes = bytes[length..];
        }

        return frames;
    }

    /// <summary>
    /// The frame of a response of the server, written the way the server writes it.
    /// </summary>
    public static byte[] ServerFrame<T>(T response) where T : IResponseHotspotMessage
    {
        return ServerFrame((ushort)response.Opcode.Value, writer => global::OpenWorker.Wire.WireBridge.Write(writer, response));
    }

    /// <summary>
    /// A frame the way a server sends it: encrypted with the server's own routine, the version of its header.
    /// </summary>
    public static byte[] ServerFrame(ushort opcode, Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(opcode);
        write(writer);
        writer.Flush();

        var body = stream.ToArray();
        var header = new MessageHeader(body.Length);

        MessageEncryption.Exchange(body.AsSpan(), header.Version);

        using var output = new MemoryStream();
        using var framed = new BinaryWriter(output);

        header.ToBinary(framed);
        framed.Write(body);
        framed.Flush();

        return output.ToArray();
    }
}
