using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Keeps the client tables the content tool has already read, so a change of panel does not read the same
/// <c>*.res</c> files again.
/// </summary>
internal sealed class ResTableCache
{
    public static ResTableCache Shared { get; } = new();

    private readonly object _gate = new();
    private string? _folder;
    private ReadOnlyCollection<MakeRow>? _makes;
    private ReadOnlyCollection<ItemRow>? _items;
    private ReadOnlyCollection<ItemScriptRow>? _scripts;
    private ReadOnlyCollection<DistrictRow>? _districts;
    private ReadOnlyCollection<MazeInfoRow>? _mazes;
    private RowIndex<int, ItemRow>? _itemsById;
    private RowIndex<int, ItemScriptRow>? _scriptsById;

    public ReadOnlyCollection<MakeRow> Makes(string folder) =>
        Get(ref _makes, folder, static path => BinaryResourceTables.LoadFromDirectory<MakeRow>(path));

    public ReadOnlyCollection<ItemRow> Items(string folder) =>
        Get(ref _items, folder, static path => BinaryResourceTables.LoadFromDirectory<ItemRow>(path));

    public ReadOnlyCollection<ItemScriptRow> Scripts(string folder) =>
        Get(ref _scripts, folder, static path => BinaryResourceTables.LoadFromDirectory<ItemScriptRow>(path));

    public ReadOnlyCollection<DistrictRow> Districts(string folder) =>
        Get(ref _districts, folder, static path => BinaryResourceTables.LoadFromDirectory<DistrictRow>(path));

    public ReadOnlyCollection<MazeInfoRow> Mazes(string folder) =>
        Get(ref _mazes, folder, static path => BinaryResourceTables.LoadFromDirectory<MazeInfoRow>(path));

    public RowIndex<int, ItemRow> ItemsById(string folder)
    {
        lock (_gate)
        {
            EnsureFolder(folder);
            return _itemsById ??= Items(folder).IndexBy(row => row.Id);
        }
    }

    public RowIndex<int, ItemScriptRow> ScriptsById(string folder)
    {
        lock (_gate)
        {
            EnsureFolder(folder);
            return _scriptsById ??= Scripts(folder).IndexBy(row => row.Id);
        }
    }

    private ReadOnlyCollection<T> Get<T>(
        ref ReadOnlyCollection<T>? field,
        string folder,
        Func<string, ReadOnlyCollection<T>> load)
        where T : struct
    {
        lock (_gate)
        {
            EnsureFolder(folder);
            return field ??= load(folder);
        }
    }

    private void EnsureFolder(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        if (string.Equals(_folder, folder, StringComparison.Ordinal))
        {
            return;
        }

        _folder = folder;
        _makes = null;
        _items = null;
        _scripts = null;
        _districts = null;
        _mazes = null;
        _itemsById = null;
        _scriptsById = null;
    }
}
