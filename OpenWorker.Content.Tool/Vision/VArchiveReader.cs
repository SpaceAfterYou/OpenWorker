using System.Globalization;
using System.Numerics;
using System.Text;

namespace OpenWorker.Content.Tool.Vision;

/// <summary>
/// Minimal Vision object archive reader for zone shape streams.
/// </summary>
internal sealed class VArchiveReader
{
    private const uint NewClassTag = 0xFFFFFFFF;
    private const uint TypeIndexFlag = 0x80000000;

    private readonly BinaryCursor _cursor;
    private readonly List<object?> _loadArray = [null];
    private readonly Dictionary<string, Action<VArchiveReader, ArchiveObject>> _serializers;
    private readonly HashSet<string> _object3dClasses;

    public VArchiveReader(
        byte[] data,
        int loadingVersion,
        bool useObjectLengths,
        bool hasPerObjectRange,
        Dictionary<string, Action<VArchiveReader, ArchiveObject>> serializers,
        HashSet<string> object3dClasses)
    {
        _cursor = new BinaryCursor(data);
        LoadingVersion = loadingVersion;
        UseObjectLengths = useObjectLengths;
        HasPerObjectRange = hasPerObjectRange;
        _serializers = serializers;
        _object3dClasses = object3dClasses;
    }

    public int LoadingVersion { get; set; }

    public bool UseObjectLengths { get; }

    public bool HasPerObjectRange { get; }

    public int? CurrentPayloadEnd { get; private set; }

    public List<ArchiveObject> Objects { get; } = [];

    public List<StaticMeshInstance> StaticMeshes { get; } = [];

    public bool Eof => _cursor.Eof;

    public int Position
    {
        get => _cursor.Position;
        set => _cursor.Position = value;
    }

    public void Seek(int position) => _cursor.Seek(position);

    public byte ReadUInt8() => _cursor.ReadUInt8();

    public sbyte ReadInt8() => _cursor.ReadInt8();

    public bool ReadBool() => _cursor.ReadBool();

    public ushort ReadUInt16() => _cursor.ReadUInt16();

    public short ReadInt16() => _cursor.ReadInt16();

    public uint ReadUInt32() => _cursor.ReadUInt32();

    public int ReadInt32() => _cursor.ReadInt32();

    public long ReadInt64() => _cursor.ReadInt64();

    public float ReadFloat() => _cursor.ReadFloat();

    public double ReadDouble() => _cursor.ReadDouble();

    public string ReadStringBinary() => DecodeCp949(_cursor);

    public string ReadVString() => ReadStringBinary();

    public uint ReadColorRef() => ReadUInt32();

    public Vector3 ReadVisVector()
    {
        var x = ReadFloat();
        var y = ReadFloat();
        var z = ReadFloat();
        _ = ReadFloat();
        return new Vector3(x, y, z);
    }

    public Vector3 ReadVec3() => new(ReadFloat(), ReadFloat(), ReadFloat());

    public Vector4 ReadVec4() => new(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());

    public Vector2 ReadVec2() => new(ReadFloat(), ReadFloat());

    public Matrix4x4 ReadMat3()
    {
        var values = new float[9];
        for (var i = 0; i < 9; i++)
        {
            values[i] = ReadFloat();
        }

        // Column-major 3x3 into a 4x4 with identity translation.
        return new Matrix4x4(
            values[0], values[3], values[6], 0f,
            values[1], values[4], values[7], 0f,
            values[2], values[5], values[8], 0f,
            0f, 0f, 0f, 1f);
    }

    public Matrix4x4 ReadMat4()
    {
        var f = new float[16];
        for (var i = 0; i < 16; i++)
        {
            f[i] = ReadFloat();
        }

        // Disk order is rows of ElementsCM with translation in column 3.
        return new Matrix4x4(
            f[0], f[1], f[2], f[3],
            f[4], f[5], f[6], f[7],
            f[8], f[9], f[10], f[11],
            f[12], f[13], f[14], f[15]);
    }

    public (Vector3 Min, Vector3 Max) ReadBBoxVis() => (ReadVisVector(), ReadVisVector());

    public (Vector3 Min, Vector3 Max) ReadBBoxX() => (ReadVec3(), ReadVec3());

    public int ReadCompressedInt()
    {
        var lead = ReadUInt8();
        var low = lead & 0x1F;
        var kind = lead & 0xE0;
        return kind switch
        {
            0x00 => low,
            0xA0 => -1 - low,
            0x20 => (low << 8) | ReadUInt8(),
            0x40 => (low << 16) | (ReadUInt8() << 8) | ReadUInt8(),
            0x60 => (low << 24) | (ReadUInt8() << 16) | (ReadUInt8() << 8) | ReadUInt8(),
            _ => unchecked((int)ReadUInt32())
        };
    }

    public void ReadEncryptedString()
    {
        var length = ReadCompressedInt();
        if (length > 0)
        {
            _ = _cursor.ReadBytes(length);
        }
    }

    public void ReadEffectConfig()
    {
        var techCount = ReadUInt16();
        for (var i = 0; i < techCount; i++)
        {
            _ = ReadStringBinary();
            _ = ReadStringBinary();
            _ = ReadStringBinary();
            _ = ReadInt32();
        }
    }

    public ArchiveObject? ReadObject()
    {
        if (HasPerObjectRange)
        {
            if (ReadBool())
            {
                _ = ReadFloat();
            }
        }

        return ReadObjectBody();
    }

    public ArchiveObject? ReadProxyObject() => ReadObject();

    public void ReadClass()
    {
        var tag = ReadUInt32();
        if (tag == 0)
        {
            return;
        }

        if ((tag & TypeIndexFlag) == 0)
        {
            throw new InvalidDataException(
                string.Format(CultureInfo.InvariantCulture, "ReadClass got object tag {0}.", tag));
        }

        _ = ReadClassTag(tag);
    }

    public ArchiveObject? ReadTypedObjectReference()
    {
        ReadClass();
        return ReadObject();
    }

    private ArchiveObject? ReadObjectBody()
    {
        var tag = ReadUInt32();
        if (tag == 0)
        {
            return null;
        }

        if ((tag & TypeIndexFlag) == 0)
        {
            return ObjectReference(checked((int)tag));
        }

        var (className, _) = ReadClassTag(tag);
        int? payloadLen = null;
        var payloadStart = _cursor.Position;
        var previousEnd = CurrentPayloadEnd;

        if (UseObjectLengths)
        {
            payloadLen = checked((int)ReadUInt32());
            payloadStart = _cursor.Position;
            CurrentPayloadEnd = payloadStart + payloadLen.Value;
        }
        else
        {
            CurrentPayloadEnd = null;
        }

        try
        {
            var obj = CreateObject(className);
            obj.ArchiveIndex = _loadArray.Count;
            _loadArray.Add(obj);
            Objects.Add(obj);

            if (!_serializers.TryGetValue(className, out var handler))
            {
                if (payloadLen is null)
                {
                    throw new InvalidDataException(
                        string.Format(CultureInfo.InvariantCulture, "No serializer for {0}.", className));
                }

                Seek(payloadStart + payloadLen.Value);
                return obj;
            }

            handler(this, obj);

            if (payloadLen is not null)
            {
                var consumed = _cursor.Position - payloadStart;
                if (consumed != payloadLen.Value)
                {
                    throw new InvalidDataException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}: object length mismatch (expected {1}, consumed {2}).",
                            className,
                            payloadLen.Value,
                            consumed));
                }
            }

            if (obj is StaticMeshArchiveObject mesh)
            {
                StaticMeshes.Add(mesh.Instance);
            }

            return obj;
        }
        finally
        {
            CurrentPayloadEnd = previousEnd;
        }
    }

    private ArchiveObject ObjectReference(int index)
    {
        if (index < 0 || index >= _loadArray.Count || _loadArray[index] is not ArchiveObject obj)
        {
            throw new InvalidDataException(
                string.Format(CultureInfo.InvariantCulture, "Bad object reference {0}.", index));
        }

        return obj;
    }

    private (string ClassName, int Schema) ReadClassTag(uint tag)
    {
        if (tag == NewClassTag)
        {
            var schema = ReadUInt16();
            var nameLen = ReadUInt16();
            var className = Encoding.ASCII.GetString(_cursor.ReadBytes(nameLen));
            _loadArray.Add(new TypeSlot(className, schema));
            return (className, schema);
        }

        var typeIndex = checked((int)(tag & ~TypeIndexFlag));
        if (typeIndex <= 0 || typeIndex >= _loadArray.Count || _loadArray[typeIndex] is not TypeSlot slot)
        {
            throw new InvalidDataException(
                string.Format(CultureInfo.InvariantCulture, "Bad type index {0}.", typeIndex));
        }

        return (slot.ClassName, slot.Schema);
    }

    private ArchiveObject CreateObject(string className)
    {
        if (className == "VisStaticMeshInstance_cl")
        {
            return new StaticMeshArchiveObject();
        }

        if (_object3dClasses.Contains(className))
        {
            return new ArchiveObject(className);
        }

        return new ArchiveObject(className);
    }

    private static string DecodeCp949(BinaryCursor cursor)
    {
        var length = cursor.ReadInt32();
        if (length <= 0)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(cursor.ReadBytes(length));
    }

    private sealed class TypeSlot(string className, int schema)
    {
        public string ClassName { get; } = className;

        public int Schema { get; } = schema;
    }
}

internal class ArchiveObject(string className)
{
    public string ClassName { get; } = className;

    public int ArchiveIndex { get; set; }

    public long UniqueId { get; set; }

    public string ObjectKey { get; set; } = string.Empty;

    public List<ArchiveObject> Components { get; } = [];
}

internal sealed class StaticMeshArchiveObject() : ArchiveObject("VisStaticMeshInstance_cl")
{
    public StaticMeshInstance Instance { get; } = new();
}
