namespace OpenWorker.Res;

/// <summary>
/// The rows of a client table by a key, made once from the table when its owner is made, so that a lookup on the world loop
/// is a dictionary probe instead of a walk through the table. The first row of a key wins, as a search from the top of the
/// table found it. A key the table does not have is an answer, <c>false</c> or the empty row, never an exception: what to do
/// about it is the caller's.
/// </summary>
public sealed class RowIndex<TKey, TRow> where TKey : notnull where TRow : struct
{
    private readonly Dictionary<TKey, TRow> _rows = [];

    public RowIndex(IEnumerable<TRow> rows, Func<TRow, TKey> key)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(key);

        foreach (var row in rows)
        {
            _rows.TryAdd(key(row), row);
        }
    }

    public int Count => _rows.Count;

    public bool TryGet(TKey key, out TRow row) => _rows.TryGetValue(key, out row);

    /// <summary>
    /// The row of the key, or the empty row (every column zero) when the table has none.
    /// </summary>
    public TRow GetOrDefault(TKey key) => _rows.GetValueOrDefault(key);
}

public static class RowIndex
{
    /// <summary>
    /// The rows by the key, the first row of a key winning.
    /// </summary>
    public static RowIndex<TKey, TRow> IndexBy<TKey, TRow>(this IEnumerable<TRow> rows, Func<TRow, TKey> key)
        where TKey : notnull where TRow : struct
    {
        return new RowIndex<TKey, TRow>(rows, key);
    }
}
