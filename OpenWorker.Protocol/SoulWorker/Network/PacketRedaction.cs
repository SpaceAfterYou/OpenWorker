using System.Buffers.Binary;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;

namespace OpenWorker.Hotspot.SoulWorker.Network;

/// <summary>
/// Masks passwords and login tickets in a decrypted message, so a dump of it can be kept or shared.
/// The masked text keeps its length, and the rest of the message stays as the client sent it.
/// </summary>
public static class PacketRedaction
{
    private const byte Mask = (byte)'*';
    private const int OpcodeSize = 2;
    private const int TicketSize = 2048;

    /// <param name="content">Message content that starts with the group and command bytes.</param>
    public static void Redact(Span<byte> content)
    {
        if (content.Length < OpcodeSize)
        {
            return;
        }

        var body = content[OpcodeSize..];

        switch ((GroupOpcode)content[0], content[1])
        {
            // Username, then password: each a 16-bit byte count and UTF-16 text.
            case (GroupOpcode.Login, (byte)LoginOpcode.Req):
                MaskUnicode(Field(body, FieldEnd(body, 0)));
                break;

            // The ticket opens a fixed block of ASCII fields.
            case (GroupOpcode.Login, (byte)LoginOpcode.FromNextHumanNetwork):
                MaskAscii(body[..Math.Min(TicketSize, body.Length)]);
                break;

            // Check type byte, then a 16-bit byte count and ASCII text.
            case (GroupOpcode.Character, (byte)CharacterOpcode.SecondPassword or (byte)CharacterOpcode.TradePassword):
                MaskAscii(Field(body, sizeof(byte)));
                break;
        }
    }

    /// <summary>
    /// Text of the field whose 16-bit length starts at <paramref name="offset"/>, cut to what the message holds.
    /// </summary>
    private static Span<byte> Field(Span<byte> body, int offset)
    {
        if (offset < 0 || offset + sizeof(short) > body.Length)
        {
            return [];
        }

        var length = BinaryPrimitives.ReadInt16LittleEndian(body[offset..]);
        var start = offset + sizeof(short);

        return length <= 0 ? [] : body.Slice(start, Math.Min(length, body.Length - start));
    }

    /// <summary>
    /// Offset right after the field that starts at <paramref name="offset"/>, or -1 when it cannot be read.
    /// </summary>
    private static int FieldEnd(ReadOnlySpan<byte> body, int offset)
    {
        if (offset + sizeof(short) > body.Length)
        {
            return -1;
        }

        var length = BinaryPrimitives.ReadInt16LittleEndian(body[offset..]);

        return length < 0 ? -1 : offset + sizeof(short) + length;
    }

    private static void MaskUnicode(Span<byte> text)
    {
        for (var i = 0; i + 1 < text.Length; i += 2)
        {
            if (text[i] == 0 && text[i + 1] == 0)
            {
                return;
            }

            text[i] = Mask;
            text[i + 1] = 0;
        }
    }

    private static void MaskAscii(Span<byte> text)
    {
        foreach (ref var value in text)
        {
            if (value != 0)
            {
                value = Mask;
            }
        }
    }
}
