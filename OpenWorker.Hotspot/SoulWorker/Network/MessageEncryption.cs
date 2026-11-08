using System.Runtime.Intrinsics;

namespace OpenWorker.Hotspot.SoulWorker.Network;

/// <summary>
/// The encryption of a message body: every byte is XORed with a key byte. The key table holds four bytes per header
/// version and a version uses the first three of its four, one after another, so the key repeats every three bytes.
/// The same operation encrypts and decrypts.
/// </summary>
public static class MessageEncryption
{
    private const int KeysPerVersion = 4;

    // Three vectors of 16 bytes: the first length that is a multiple of both the vector and the period of the key.
    private const int Block = 3 * 16;

    // A constant array behind a ReadOnlySpan is read straight from the data of the assembly: no array on the heap.
    private static ReadOnlySpan<byte> KeyTable =>
    [
        0x57, 0x19, 0xC6, 0x2D, 0x56, 0x68, 0x3A, 0xCC,
        0x60, 0x3B, 0x0B, 0xB1, 0x90, 0x5C, 0x4A, 0xF8,
        0x80, 0x28, 0xB1, 0x45, 0xB6, 0x85, 0xE7, 0x4C,
        0x06, 0x2D, 0x55, 0x83, 0xAF, 0x44, 0x99, 0x95,
        0xD9, 0x98, 0xBF, 0xAE, 0x53, 0x43, 0x63, 0xC8,
        0x4A, 0x71, 0x80, 0x9D, 0x0B, 0xA1, 0x70, 0x8A,
        0x0F, 0x54, 0x9C, 0x1B, 0x06, 0xC0, 0xEA, 0x3C,
        0xC0, 0x88, 0x71, 0x48, 0xB3, 0xB9, 0x45, 0x78,
        0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0x7F, 0x7F, 0xFF,
        0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0xFF,
        0x00, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
        0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF,
        0xFF, 0x00, 0xFF, 0xFF
    ];

    /// <summary>
    /// The key of every version repeated over one <see cref="Block"/>, built from <see cref="KeyTable"/> once.
    /// </summary>
    private static readonly byte[] Blocks = CreateBlocks();

    private static byte[] CreateBlocks()
    {
        var versions = KeyTable.Length / KeysPerVersion;
        var blocks = new byte[versions * Block];

        for (var version = 0; version < versions; version++)
        {
            for (var index = 0; index < Block; index++)
            {
                blocks[version * Block + index] = KeyTable[version * KeysPerVersion + index % 3];
            }
        }

        return blocks;
    }

    /// <summary>
    /// Encrypts or decrypts the bytes in place.
    /// </summary>
    /// <param name="version">The version of the header; one the table has no keys for throws, unless there is nothing to do.</param>
    public static void Exchange(Span<byte> buffer, ushort version)
    {
        if (buffer.IsEmpty)
        {
            return;
        }

        var key = KeyTable.Slice(version * KeysPerVersion, 3);

        // The three keys stay in registers, and the loop has no division by three.
        var k0 = key[0];
        var k1 = key[1];
        var k2 = key[2];

        var index = 0;

        // A block is a whole number of periods of the key, so every block takes the same three vectors, and the bytes
        // after the last block start the key from its first byte again.
        if (Vector128.IsHardwareAccelerated && buffer.Length >= Block)
        {
            var keys = Blocks.AsSpan(version * Block, Block);

            var first = Vector128.Create<byte>(keys);
            var second = Vector128.Create<byte>(keys[16..]);
            var third = Vector128.Create<byte>(keys[32..]);

            for (; index + Block <= buffer.Length; index += Block)
            {
                var block = buffer.Slice(index, Block);

                (Vector128.Create<byte>(block) ^ first).CopyTo(block);
                (Vector128.Create<byte>(block[16..]) ^ second).CopyTo(block[16..]);
                (Vector128.Create<byte>(block[32..]) ^ third).CopyTo(block[32..]);
            }
        }

        for (; index + 2 < buffer.Length; index += 3)
        {
            buffer[index] ^= k0;
            buffer[index + 1] ^= k1;
            buffer[index + 2] ^= k2;
        }

        // One or two bytes are left over when the length is not a multiple of three.
        if (index < buffer.Length)
        {
            buffer[index] ^= k0;
        }

        if (index + 1 < buffer.Length)
        {
            buffer[index + 1] ^= k1;
        }
    }

    public static void Exchange(Memory<byte> buffer, ushort version)
    {
        Exchange(buffer.Span, version);
    }
}
