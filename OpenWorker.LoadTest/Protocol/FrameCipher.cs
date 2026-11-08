using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.LoadTest.Protocol;

/// <summary>
/// The encryption of a frame body: every byte is XORed with a key byte, and the key bytes repeat every three
/// bytes. Which three depends on the version in the header. The key table is the server's own
/// (<see cref="MessageEncryption" />): the bot asks it for the three bytes of a version once, by encrypting zeros,
/// and then applies them itself without the enumerator and the interface calls of the server's loop.
/// </summary>
internal static class FrameCipher
{
    /// <summary>
    /// The newest version the key table holds keys for (four table bytes per version, three of them used).
    /// </summary>
    public const ushort MaxVersion = 24;

    private static readonly byte[]?[] Keys = new byte[MaxVersion + 1][];

    public static bool IsKnown(ushort version) => version <= MaxVersion;

    /// <summary>
    /// Encrypts or decrypts: the same operation does both.
    /// </summary>
    public static void Apply(Span<byte> body, ushort version)
    {
        var key = KeyOf(version);

        // Three strides keep the key bytes in registers and leave no division by three in the loop.
        var k0 = key[0];
        var k1 = key[1];
        var k2 = key[2];

        var i = 0;

        for (; i + 2 < body.Length; i += 3)
        {
            body[i] ^= k0;
            body[i + 1] ^= k1;
            body[i + 2] ^= k2;
        }

        if (i < body.Length)
        {
            body[i] ^= k0;
        }

        if (i + 1 < body.Length)
        {
            body[i + 1] ^= k1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] KeyOf(ushort version)
    {
        return version <= MaxVersion ? Keys[version] ?? Derive(version) : ThrowUnknown(version);
    }

    private static byte[] Derive(ushort version)
    {
        var key = new byte[3];

        MessageEncryption.Exchange(key.AsSpan(), version);

        Keys[version] = key;

        return key;
    }

    private static byte[] ThrowUnknown(ushort version)
    {
        throw new InvalidDataException($"The frame has the version {version}, and the key table has no keys for it.");
    }
}
