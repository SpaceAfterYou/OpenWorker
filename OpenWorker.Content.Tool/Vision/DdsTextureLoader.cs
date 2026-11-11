using System.Buffers.Binary;
using System.Globalization;

namespace OpenWorker.Content.Tool.Vision;

/// <summary>
/// Loads DXT1 / DXT3 / DXT5 <c>.dds</c> files for compressed OpenGL upload.
/// </summary>
internal static class DdsTextureLoader
{
    private const uint DdsMagic = 0x20534444; // 'DDS '
    private const uint FourCcDxt1 = 0x31545844; // 'DXT1'
    private const uint FourCcDxt3 = 0x33545844; // 'DXT3'
    private const uint FourCcDxt5 = 0x35545844; // 'DXT5'

    // Desktop OpenGL compressed format enums (S3TC).
    public const int GlCompressedRgbS3tcDxt1 = 0x83F0;
    public const int GlCompressedRgbaS3tcDxt1 = 0x83F1;
    public const int GlCompressedRgbaS3tcDxt3 = 0x83F2;
    public const int GlCompressedRgbaS3tcDxt5 = 0x83F3;

    public readonly record struct DdsImage(int Width, int Height, int GlInternalFormat, byte[] Payload);

    public static DdsImage Load(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length < 128 || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0, 4)) != DdsMagic)
        {
            throw new InvalidDataException(
                string.Format(CultureInfo.InvariantCulture, "Not a DDS file: {0}", path));
        }

        var height = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(12, 4));
        var width = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(16, 4));
        var pfFlags = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(80, 4));
        var fourCc = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(84, 4));
        if ((pfFlags & 0x4) == 0)
        {
            throw new InvalidDataException(
                string.Format(CultureInfo.InvariantCulture, "DDS without FOURCC is unsupported: {0}", path));
        }

        var (format, blockSize) = fourCc switch
        {
            FourCcDxt1 => (GlCompressedRgbaS3tcDxt1, 8),
            FourCcDxt3 => (GlCompressedRgbaS3tcDxt3, 16),
            FourCcDxt5 => (GlCompressedRgbaS3tcDxt5, 16),
            _ => throw new InvalidDataException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Unsupported DDS compression {0:X8} in {1}",
                    fourCc,
                    path))
        };

        var blocksWide = Math.Max(1, (width + 3) / 4);
        var blocksHigh = Math.Max(1, (height + 3) / 4);
        var size = blocksWide * blocksHigh * blockSize;
        if (128 + size > data.Length)
        {
            throw new InvalidDataException(
                string.Format(CultureInfo.InvariantCulture, "DDS payload truncated: {0}", path));
        }

        var payload = data.AsSpan(128, size).ToArray();
        return new DdsImage(width, height, format, payload);
    }
}
