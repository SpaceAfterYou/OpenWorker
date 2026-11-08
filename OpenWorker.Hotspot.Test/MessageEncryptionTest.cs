using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class MessageEncryptionTest
{
    /// <summary>
    /// The key table as the first version of the server had it, the oracle of the test.
    /// </summary>
    private static readonly byte[] OracleKeys =
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
    /// The formula of the first version, byte by byte.
    /// </summary>
    private static void OracleExchange(byte[] buffer, ushort version)
    {
        for (var index = 0; index < buffer.Length; index++)
        {
            buffer[index] ^= OracleKeys[index % 3 + version * 4];
        }
    }

    /// <summary>
    /// The loop over the key table changes the bytes exactly as the formula of the first version did, for every version the
    /// table has keys for (the server writes version 5) and every length up to 70: the byte loop alone below 48 bytes, one
    /// block of vectors and the bytes after it from 48 on.
    /// </summary>
    [TestMethod]
    public void TheLoopGivesTheBytesOfTheFormula()
    {
        var random = new Random(1);

        for (ushort version = 0; version * 4 + 2 < OracleKeys.Length; version++)
        {
            for (var length = 0; length <= 70; length++)
            {
                var plain = new byte[length];

                random.NextBytes(plain);

                var expected = plain.ToArray();
                var actual = plain.ToArray();

                OracleExchange(expected, version);
                MessageEncryption.Exchange(actual.AsSpan(), version);

                CollectionAssert.AreEqual(expected, actual, $"Version {version}, length {length}.");
            }
        }
    }
}
