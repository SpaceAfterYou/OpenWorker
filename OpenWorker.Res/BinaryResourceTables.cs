using System.Collections.ObjectModel;
using System.Reflection;
using OpenWorker.Res.Attributes;

namespace OpenWorker.Res;

/// <summary>
/// Reads a table of rows that carry <see cref="BinaryResourceTableAttribute"/>: a leading <c>int</c> count, then that many
/// rows. The servers load every such table through <c>AddRes</c>; tools and tests may load one table from a folder of
/// <c>*.res</c> files.
/// </summary>
public static class BinaryResourceTables
{
    /// <summary>
    /// The table of <typeparamref name="T"/> from <see cref="BinaryResourceTableAttribute.PathIn"/> under
    /// <paramref name="root"/>, or an empty collection when that path is missing for an optional or server-only table.
    /// </summary>
    public static ReadOnlyCollection<T> Load<T>(string root = "") where T : struct
    {
        var attribute = RequireAttribute(typeof(T));
        var path = attribute.PathIn(root);

        return path is null ? Empty<T>() : Read<T>(path);
    }

    /// <summary>
    /// The table of <typeparamref name="T"/> from a folder that already holds the <c>*.res</c> files (the layout of
    /// <c>OPENWORKER_TEST_RES</c>), named by its <see cref="BinaryResourceTableAttribute"/>.
    /// </summary>
    public static ReadOnlyCollection<T> LoadFromDirectory<T>(string directory) where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var attribute = RequireAttribute(typeof(T));
        return Read<T>(Path.Join(directory, attribute.Table + ".res"));
    }

    /// <summary>
    /// The rows of a table file, as the type that reads them.
    /// </summary>
    public static ReadOnlyCollection<T> Read<T>(string path) where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return new ReadOnlyCollection<T>((T[])ReadArray(typeof(T), path));
    }

    /// <summary>
    /// The rows of a table file as a typed array, for registration into a service collection.
    /// </summary>
    public static Array ReadArray(Type type, string path)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var buffer = File.ReadAllBytes(path);

        using var stream = new MemoryStream(buffer);
        using var reader = new BinaryReader(stream);

        var count = reader.ReadInt32();
        var array = Array.CreateInstance(type, count);

        for (var index = 0; index < count; index++)
        {
            var value = Activator.CreateInstance(type, reader);
            ArgumentNullException.ThrowIfNull(value);
            array.SetValue(value, index);
        }

        return array;
    }

    /// <summary>
    /// An empty read-only collection of the row type, for an optional table that is not on disk.
    /// </summary>
    public static ReadOnlyCollection<T> Empty<T>() where T : struct =>
        new((T[])Array.CreateInstance(typeof(T), 0));

    /// <summary>
    /// An empty read-only collection of the row type, boxed for registration into a service collection.
    /// </summary>
    public static object EmptyCollection(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var empty = typeof(ReadOnlyCollection<>).MakeGenericType(type);
        return Activator.CreateInstance(empty, Array.CreateInstance(type, 0))!;
    }

    /// <summary>
    /// A read-only collection around a typed array of rows, boxed for registration into a service collection.
    /// </summary>
    public static object WrapCollection(Type type, Array rows)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(rows);

        var collectionType = typeof(ReadOnlyCollection<>).MakeGenericType(type);
        return Activator.CreateInstance(collectionType, rows)!;
    }

    private static BinaryResourceTableAttribute RequireAttribute(Type type)
    {
        var attribute = type.GetCustomAttribute<BinaryResourceTableAttribute>();
        if (attribute is null)
        {
            throw new InvalidOperationException(
                $"Type {type.FullName} is not a resource table: add [BinaryResourceTable].");
        }

        return attribute;
    }
}
