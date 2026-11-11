using System.Numerics;

namespace OpenWorker.Content.Tool.Vision;

/// <summary>
/// Reads triangle positions from a Vision <c>*.vmesh</c> (<c>VBIN</c> / <c>VMSH</c>).
/// </summary>
internal static class VMeshReader
{
    private const int VbinMagic = 0x4E494256; // 'VBIN' as little-endian int of big-endian fourcc
    private const int VmshId = 0x564D5348; // 'VMSH'
    private const uint DescriptorMagic = 0x1020A0B;
    private const uint MeshMagic = 0x4455ABCD;
    private const ushort UnsetOffset = 0xFFFF;
    private const ushort OffsetMask = 0x0FFF;

    public readonly record struct MeshGeometry(Vector3[] Positions, int[] Indices);

    public static MeshGeometry Load(string path)
    {
        var data = File.ReadAllBytes(path);
        var cursor = new BinaryCursor(data);
        var headerId = cursor.ReadChunkId();
        if (headerId != VbinMagic && headerId != unchecked((int)0x5642494E))
        {
            // Accept either byte order of the VBIN fourcc.
            var fourcc = BitConverter.GetBytes(headerId);
            if (!(fourcc[0] == (byte)'V' && fourcc[1] == (byte)'B' && fourcc[2] == (byte)'I' && fourcc[3] == (byte)'N')
                && !(fourcc[0] == (byte)'N' && fourcc[1] == (byte)'I' && fourcc[2] == (byte)'B' && fourcc[3] == (byte)'V'))
            {
                throw new InvalidDataException($"Not a Vision binary mesh: {path}");
            }
        }

        _ = cursor.ReadUInt32(); // bin version

        MeshGeometry? found = null;
        while (!cursor.Eof)
        {
            var depth = cursor.ReadInt32();
            if (depth < 0)
            {
                break;
            }

            var chunkId = cursor.ReadChunkId();
            var length = checked((int)cursor.ReadUInt32());
            var start = cursor.Position;
            if (chunkId == VmshId || IsFourCc(chunkId, (byte)'V', (byte)'M', (byte)'S', (byte)'H'))
            {
                found = ReadVmsh(cursor, start, length);
            }

            cursor.Seek(start + length);
            _ = cursor.ReadInt32(); // exit depth delta
            _ = cursor.ReadChunkId(); // exit chunk id
        }

        return found ?? throw new InvalidDataException($"No VMSH chunk in {path}");
    }

    private static bool IsFourCc(int value, byte a, byte b, byte c, byte d)
    {
        var bytes = BitConverter.GetBytes(value);
        return (bytes[0] == a && bytes[1] == b && bytes[2] == c && bytes[3] == d)
            || (bytes[0] == d && bytes[1] == c && bytes[2] == b && bytes[3] == a);
    }

    private static MeshGeometry ReadVmsh(BinaryCursor cursor, int start, int length)
    {
        var end = start + length;
        var innerId = cursor.ReadChunkId();
        if (!(innerId == VmshId || IsFourCc(innerId, (byte)'V', (byte)'M', (byte)'S', (byte)'H')))
        {
            throw new InvalidDataException("VMSH payload missing inner chunk id.");
        }

        var loaderVersion = cursor.ReadUInt32();
        if (loaderVersion != 1)
        {
            throw new InvalidDataException($"Unsupported VMSH loader version {loaderVersion}.");
        }

        var magic = cursor.ReadUInt32();
        if (magic != MeshMagic)
        {
            throw new InvalidDataException("VMSH magic mismatch.");
        }

        var version = cursor.ReadUInt32();
        if (version > 5)
        {
            throw new InvalidDataException($"Unsupported VMSH version {version}.");
        }

        var (stride, posOffset, hasPosition) = ReadDescriptor(cursor);
        var vertexCount = checked((int)cursor.ReadUInt32());
        _ = cursor.ReadUInt8(); // usage vertices
        if (version >= 4)
        {
            _ = cursor.ReadUInt8(); // bind vertices
        }

        if (version >= 3)
        {
            _ = cursor.ReadBool();
            _ = cursor.ReadUInt16();
        }

        _ = cursor.ReadUInt32(); // prim type
        var indexCount = checked((int)cursor.ReadUInt32());
        var indexFormat = cursor.ReadUInt32();
        _ = cursor.ReadUInt32(); // prim count
        _ = cursor.ReadUInt8(); // mem usage indices
        if (version >= 4)
        {
            _ = cursor.ReadUInt8(); // bind indices
        }

        _ = cursor.ReadUInt8();
        _ = cursor.ReadUInt8();
        if (version >= 5)
        {
            _ = cursor.ReadUInt8();
        }

        // Render state: transparency, unused, flags
        _ = cursor.ReadUInt8();
        _ = cursor.ReadUInt8();
        _ = cursor.ReadUInt16();

        _ = cursor.ReadUInt8(); // projection
        var textureChannels = cursor.ReadUInt8();
        for (var i = 0; i < textureChannels; i++)
        {
            _ = cursor.ReadUtf8UInt32();
        }

        ReadEffectConfig(cursor);

        if (!hasPosition)
        {
            throw new InvalidDataException("VMSH has no position stream.");
        }

        var positions = new Vector3[vertexCount];
        var vertexStart = cursor.Position;
        for (var i = 0; i < vertexCount; i++)
        {
            var baseOffset = vertexStart + i * stride;
            cursor.Seek(baseOffset + posOffset);
            positions[i] = new Vector3(cursor.ReadFloat(), cursor.ReadFloat(), cursor.ReadFloat());
        }

        cursor.Seek(vertexStart + stride * vertexCount);

        var indices = new int[indexCount];
        if (indexFormat == 16)
        {
            for (var i = 0; i < indexCount; i++)
            {
                indices[i] = cursor.ReadUInt16();
            }
        }
        else if (indexFormat == 32)
        {
            for (var i = 0; i < indexCount; i++)
            {
                indices[i] = checked((int)cursor.ReadUInt32());
            }
        }
        else
        {
            throw new InvalidDataException($"Unknown VMSH index format {indexFormat}.");
        }

        // Bounding box + unused int; ignore remainder of chunk.
        if (cursor.Position + 28 <= end)
        {
            cursor.Seek(Math.Min(end, cursor.Position + 28));
        }

        cursor.Seek(end);
        return new MeshGeometry(positions, indices);
    }

    private static (int Stride, int PosOffset, bool HasPosition) ReadDescriptor(BinaryCursor cursor)
    {
        var magick = cursor.ReadUInt32();
        if (magick != DescriptorMagic)
        {
            throw new InvalidDataException("Vertex descriptor magic mismatch.");
        }

        var headerSize = cursor.ReadUInt32();
        var stride = cursor.ReadUInt16();
        var posOffset = cursor.ReadUInt16();
        _ = cursor.ReadUInt16(); // color
        _ = cursor.ReadUInt16(); // normal
        for (var i = 0; i < 16; i++)
        {
            _ = cursor.ReadUInt16();
        }

        _ = cursor.ReadUInt16(); // secondary color
        if (headerSize == 48)
        {
            _ = cursor.ReadUInt8();
            _ = cursor.ReadUInt8();
            _ = cursor.ReadUInt32();
        }

        _ = cursor.ReadUInt32(); // trailing magic
        var hasPosition = posOffset != UnsetOffset;
        return (stride, posOffset & OffsetMask, hasPosition);
    }

    private static void ReadEffectConfig(BinaryCursor cursor)
    {
        var count = cursor.ReadUInt16();
        for (var i = 0; i < count; i++)
        {
            // Leaf effect: three length-prefixed UTF-8 strings + flags.
            // Nested count wrappers also start with a u16; when the outer count
            // is zero (common for static props) we never enter this loop.
            _ = cursor.ReadUtf8UInt32();
            _ = cursor.ReadUtf8UInt32();
            _ = cursor.ReadUtf8UInt32();
            _ = cursor.ReadUInt32();
        }
    }
}
