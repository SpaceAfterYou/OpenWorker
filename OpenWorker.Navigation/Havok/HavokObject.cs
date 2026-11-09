namespace OpenWorker.Navigation.Havok;

/// <summary>
/// A pointer to another object of the file, the first object being number 1. Zero is nothing.
/// </summary>
public readonly record struct HavokReference(int Index);

/// <summary>
/// One object of a tagfile with its members by name, the members of the base classes among them.
/// <para>
/// A byte comes as <see cref="byte"/>, an integer as <see cref="long"/>, a real as <see cref="float"/>, a string as
/// <see cref="string"/>, a vector as <c>float[]</c>, a pointer as <see cref="HavokReference"/> and a struct as another
/// <see cref="HavokObject"/>. An array of them comes as an array of the same kind (<c>byte[]</c>, <c>long[]</c>,
/// <c>float[]</c>, <c>float[][]</c> for vectors, <c>string[]</c>, <see cref="HavokObject"/><c>[]</c>).
/// A member that is not in the file has the default value and is not in the dictionary.
/// </para>
/// </summary>
public sealed class HavokObject(string className)
{
    private readonly Dictionary<string, object?> _members = [];

    public string ClassName { get; } = className;

    public IReadOnlyDictionary<string, object?> Members => _members;

    internal void Set(string name, object? value)
    {
        _members[name] = value;
    }

    public long GetInt(string name, long fallback = 0)
    {
        return _members.TryGetValue(name, out var value) && value is long number ? number : fallback;
    }

    public T[] GetArray<T>(string name)
    {
        return _members.TryGetValue(name, out var value) && value is T[] array ? array : [];
    }

    public HavokObject? GetStruct(string name)
    {
        return _members.TryGetValue(name, out var value) ? value as HavokObject : null;
    }

    public override string ToString() => ClassName;
}

/// <summary>
/// A binary tagfile of the Havok SDK that was loaded: its objects in the order the file keeps them.
/// </summary>
public sealed class HavokTagfile(IReadOnlyList<HavokObject> objects)
{
    public IReadOnlyList<HavokObject> Objects { get; } = objects;

    /// <summary>
    /// The first object of the file, the one all the others hang on.
    /// </summary>
    public HavokObject? Root => Objects.Count > 0 ? Objects[0] : null;

    public HavokObject? Find(string className)
    {
        return Objects.FirstOrDefault(e => e.ClassName == className);
    }

    public HavokObject? Resolve(HavokReference reference)
    {
        return reference.Index >= 1 && reference.Index <= Objects.Count ? Objects[reference.Index - 1] : null;
    }

    public static HavokTagfile Parse(ReadOnlySpan<byte> data)
    {
        return new HavokTagfileReader(data).Read();
    }

    public static HavokTagfile Load(string path)
    {
        return Parse(File.ReadAllBytes(path));
    }
}
