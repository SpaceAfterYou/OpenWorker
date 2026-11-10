namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Reads a client <c>*.res</c> table: a leading <c>int</c> count, then that many rows.
/// </summary>
internal static class ResTableLoader
{
    public static IReadOnlyList<T> Load<T>(string folder, string table)
    {
        var path = Path.Combine(folder, table + ".res");
        using var reader = new BinaryReader(File.OpenRead(path));
        var count = reader.ReadInt32();
        var rows = new T[count];

        for (var i = 0; i < count; i++)
        {
            rows[i] = (T)Activator.CreateInstance(typeof(T), reader)!;
        }

        return rows;
    }
}
