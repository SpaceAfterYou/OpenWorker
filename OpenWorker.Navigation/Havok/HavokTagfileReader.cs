using System.Buffers.Binary;
using System.Text;

namespace OpenWorker.Navigation.Havok;

/// <summary>
/// Reads the binary tagfile the Havok SDK saves its objects in (the files of the navigation meshes are such files).
/// <para>
/// The file starts with two magic numbers. Then come tags, each a packed number: file information (the version of the
/// format and the version of the SDK), the description of a class (name, version, base class, members with their types),
/// an object (the class, a bit for every member that is in the file, the values of these members), and the end.
/// A packed number keeps six bits in the first byte after the sign, and seven in every next; a set top bit says there
/// are more bytes. A string is a packed length and the characters, and a length that is not positive is the place of a
/// string that has been read already (the first two places are empty). Objects that other objects point at are
/// numbered from one in the order they are met.
/// </para>
/// <para>
/// The members of an array of structs are written by columns: a bit for every member, and then all the values of the
/// first member that is in the file, all of the second one, and so on.
/// </para>
/// </summary>
internal sealed class HavokTagfileReader
{
    private const uint MagicFirst = 0xCAB00D1E;
    private const uint MagicSecond = 0xD011FACE;

    private const int TagFileInfo = 1;
    private const int TagMetadata = 2;
    private const int TagObject = 3;
    private const int TagObjectRemember = 4;
    private const int TagObjectNull = 6;
    private const int TagFileEnd = 7;

    private const int MaxNesting = 32;

    private enum Basic
    {
        Void = 0,
        Byte = 1,
        Int = 2,
        Real = 3,
        Vec4 = 4,
        Vec8 = 5,
        Vec12 = 6,
        Vec16 = 7,
        Object = 8,
        Struct = 9,
        CString = 10
    }

    private sealed class Member(string name, Basic basic, bool isArray, bool isTuple, int tupleSize, string? className)
    {
        public string Name { get; } = name;
        public Basic Basic { get; } = basic;
        public bool IsArray { get; } = isArray;
        public bool IsTuple { get; } = isTuple;
        public int TupleSize { get; } = tupleSize;
        public string? ClassName { get; } = className;

        public bool IsVector => Basic is >= Basic.Vec4 and <= Basic.Vec16 || IsTuple && Basic == Basic.Real && TupleSize is 4 or 8 or 12 or 16;

        public int VectorSize => Basic switch
        {
            Basic.Vec4 => 4,
            Basic.Vec8 => 8,
            Basic.Vec12 => 12,
            Basic.Vec16 => 16,
            _ => TupleSize
        };
    }

    private sealed class ClassInfo(string name, ClassInfo? parent)
    {
        public string Name { get; } = name;
        public List<Member> Own { get; } = [];

        public List<Member> All { get; } = parent is null ? [] : [.. parent.All];
    }

    private readonly byte[] _data;
    private readonly List<string?> _strings = [null, null];
    private readonly List<ClassInfo?> _classes = [null];
    private readonly Dictionary<string, ClassInfo> _classesByName = [];
    private readonly List<HavokObject> _objects = [];

    private int _position;
    private int _version;

    public HavokTagfileReader(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
    }

    public HavokTagfile Read()
    {
        if (_data.Length < 8 ||
            BinaryPrimitives.ReadUInt32LittleEndian(_data) != MagicFirst ||
            BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(4)) != MagicSecond)
        {
            throw new InvalidDataException("This is not a binary tagfile of Havok.");
        }

        _position = 8;

        while (true)
        {
            var tag = (int)ReadPacked();

            switch (tag)
            {
                case TagFileInfo:
                    _version = (int)ReadPacked();

                    if (_version is < 2 or > 4)
                    {
                        throw new InvalidDataException($"The version {_version} of the tagfile is not supported.");
                    }

                    if (_version == 4)
                    {
                        ReadString();
                    }

                    break;

                case TagMetadata:
                    ReadClass();
                    break;

                case TagObject or TagObjectRemember:
                    ReadObject(tag == TagObjectRemember, null, 0);
                    break;

                case TagObjectNull:
                    break;

                case TagFileEnd:
                    return new HavokTagfile(_objects);

                default:
                    throw new InvalidDataException("A tag of the tagfile is not known.");
            }
        }
    }

    // ---- Numbers and strings ------------------------------------------------------------------

    private byte ReadByte()
    {
        if (_position >= _data.Length)
        {
            throw new InvalidDataException("The tagfile ends too early.");
        }

        return _data[_position++];
    }

    private long ReadPacked()
    {
        var first = ReadByte();
        var value = (long)((first >> 1) & 0x3F);
        var negative = (first & 1) != 0;
        var bits = 6;

        if ((first & 0x80) != 0)
        {
            byte next;

            do
            {
                next = ReadByte();

                if (bits > 56)
                {
                    throw new InvalidDataException("A number of the tagfile is too long.");
                }

                value |= (long)(next & 0x7F) << bits;
                bits += 7;
            } while ((next & 0x80) != 0);
        }

        return negative ? -value : value;
    }

    private int ReadCount()
    {
        var value = ReadPacked();

        if (value < 0 || value > _data.Length * 8L)
        {
            throw new InvalidDataException("A count of the tagfile is not right.");
        }

        return (int)value;
    }

    private string? ReadString()
    {
        var length = ReadPacked();

        if (length <= 0)
        {
            var index = (int)-length;

            return index < _strings.Count ? _strings[index] : throw new InvalidDataException("A string of the tagfile points nowhere.");
        }

        if (length > _data.Length - _position)
        {
            throw new InvalidDataException("A string of the tagfile ends too late.");
        }

        var text = Encoding.Latin1.GetString(_data, _position, (int)length);

        _position += (int)length;
        _strings.Add(text);

        return text;
    }

    private float[] ReadFloats(int count)
    {
        if (count < 0 || count > (_data.Length - _position) / 4)
        {
            throw new InvalidDataException("The reals of the tagfile end too early.");
        }

        var result = new float[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = BinaryPrimitives.ReadSingleLittleEndian(_data.AsSpan(_position));
            _position += 4;
        }

        return result;
    }

    // ---- Classes ------------------------------------------------------------------------------

    private void ReadClass()
    {
        var name = ReadString() ?? throw new InvalidDataException("A class of the tagfile has no name.");

        ReadPacked();

        var parentIndex = (int)ReadPacked();
        var parent = parentIndex > 0 && parentIndex < _classes.Count ? _classes[parentIndex] : null;
        var members = ReadCount();
        var info = new ClassInfo(name, parent);

        for (var i = 0; i < members; i++)
        {
            var memberName = ReadString() ?? string.Empty;
            var flags = (int)ReadPacked();
            var tupleSize = (flags & 0x20) != 0 ? (int)ReadPacked() : 0;
            var basic = (Basic)(flags & 0xF);
            var className = basic is Basic.Object or Basic.Struct ? ReadString() : null;

            info.Own.Add(new Member(memberName, basic, (flags & 0x10) != 0, (flags & 0x20) != 0, tupleSize, className));
        }

        info.All.AddRange(info.Own);

        _classes.Add(info);
        _classesByName[name] = info;
    }

    // ---- Objects ------------------------------------------------------------------------------

    private HavokObject ReadObject(bool remember, ClassInfo? known, int depth)
    {
        if (depth > MaxNesting)
        {
            throw new InvalidDataException("The objects of the tagfile are nested too deep.");
        }

        var info = known;

        if (info is null)
        {
            var index = (int)ReadPacked();

            info = index > 0 && index < _classes.Count ? _classes[index] : null;

            if (info is null)
            {
                throw new InvalidDataException("An object of the tagfile has a class that is not known.");
            }
        }

        var result = new HavokObject(info.Name);

        if (remember)
        {
            _objects.Add(result);
        }

        var present = ReadBitfield(info.All.Count);

        for (var i = 0; i < info.All.Count; i++)
        {
            if (present[i])
            {
                result.Set(info.All[i].Name, ReadMember(info.All[i], depth + 1));
            }
        }

        return result;
    }

    private bool[] ReadBitfield(int count)
    {
        var bytes = (count + 7) / 8;

        if (bytes > _data.Length - _position)
        {
            throw new InvalidDataException("The bits of the tagfile end too early.");
        }

        var result = new bool[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = (_data[_position + i / 8] >> (i % 8) & 1) != 0;
        }

        _position += bytes;

        return result;
    }

    private object? ReadMember(Member member, int depth)
    {
        if (member.IsArray)
        {
            return ReadItems(member, ReadCount(), depth);
        }

        if (member.IsTuple && !member.IsVector)
        {
            return ReadItems(member, member.TupleSize, depth);
        }

        return ReadSingle(member, depth);
    }

    private object? ReadSingle(Member member, int depth)
    {
        if (member.IsVector)
        {
            return ReadFloats(member.VectorSize);
        }

        return member.Basic switch
        {
            Basic.Void => null,
            Basic.Byte => ReadByte(),
            Basic.Real => ReadFloats(1)[0],
            Basic.Int => ReadPacked(),
            Basic.CString => ReadString(),
            Basic.Object => new HavokReference((int)ReadPacked()),
            Basic.Struct => ReadObject(false, FindClass(member), depth),
            _ => throw new InvalidDataException("A member of the tagfile has a type that is not known.")
        };
    }

    private ClassInfo FindClass(Member member)
    {
        return member.ClassName is { } name && _classesByName.TryGetValue(name, out var info)
            ? info
            : throw new InvalidDataException("A member of the tagfile has a class that is not known.");
    }

    /// <summary>
    /// Reads count values of the type of the member, written one after another.
    /// </summary>
    private object? ReadItems(Member member, int count, int depth)
    {
        if (depth > MaxNesting)
        {
            throw new InvalidDataException("The objects of the tagfile are nested too deep.");
        }

        if (member.IsVector)
        {
            var size = member.VectorSize;
            var stored = size;

            // Four reals say how many of them are in the file: the last one is often not.
            if (size == 4)
            {
                stored = ReadCount();
            }

            var flat = ReadFloats(checked(count * stored));
            var vectors = new float[count][];

            for (var i = 0; i < count; i++)
            {
                vectors[i] = new float[size];
                Array.Copy(flat, i * stored, vectors[i], 0, Math.Min(stored, size));
            }

            return vectors;
        }

        switch (member.Basic)
        {
            case Basic.Byte:
            {
                if (count > _data.Length - _position)
                {
                    throw new InvalidDataException("The bytes of the tagfile end too early.");
                }

                var bytes = _data.AsSpan(_position, count).ToArray();

                _position += count;

                return bytes;
            }

            case Basic.Real:
                return ReadFloats(count);

            case Basic.Int:
            {
                if (_version >= 3)
                {
                    ReadPacked();
                }

                var numbers = new long[Reserve(count)];

                for (var i = 0; i < count; i++)
                {
                    numbers[i] = ReadPacked();
                }

                return numbers;
            }

            case Basic.CString:
            {
                var strings = new string?[Reserve(count)];

                for (var i = 0; i < count; i++)
                {
                    strings[i] = ReadString();
                }

                return strings;
            }

            case Basic.Object:
            {
                var references = new HavokReference[Reserve(count)];

                for (var i = 0; i < count; i++)
                {
                    references[i] = new HavokReference((int)ReadPacked());
                }

                return references;
            }

            case Basic.Struct:
                return ReadColumns(FindClass(member), count, depth);

            case Basic.Void:
                return null;

            default:
                throw new InvalidDataException("An array of the tagfile has a type that is not known.");
        }
    }

    private int Reserve(int count)
    {
        // Every value takes a byte at least, so a count beyond the rest of the file is a broken file.
        return count <= _data.Length - _position ? count : throw new InvalidDataException("The values of the tagfile end too early.");
    }

    private HavokObject[] ReadColumns(ClassInfo info, int count, int depth)
    {
        var present = ReadBitfield(info.All.Count);
        var rows = new HavokObject[Reserve(count)];

        for (var i = 0; i < count; i++)
        {
            rows[i] = new HavokObject(info.Name);
        }

        for (var i = 0; i < info.All.Count; i++)
        {
            if (!present[i])
            {
                continue;
            }

            var member = info.All[i];

            if (member.IsArray)
            {
                foreach (var row in rows)
                {
                    row.Set(member.Name, ReadItems(member, ReadCount(), depth + 1));
                }
            }
            else if (member.IsTuple && !member.IsVector)
            {
                foreach (var row in rows)
                {
                    row.Set(member.Name, ReadItems(member, member.TupleSize, depth + 1));
                }
            }
            else
            {
                var column = ReadItems(member, count, depth + 1) as Array;

                for (var row = 0; row < count && column is not null; row++)
                {
                    rows[row].Set(member.Name, column.GetValue(row));
                }
            }
        }

        return rows;
    }
}
