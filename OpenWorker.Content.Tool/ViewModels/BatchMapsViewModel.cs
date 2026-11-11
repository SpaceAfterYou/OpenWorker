using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenWorker.Content.Tool.Data;
using OpenWorker.Content.Tool.Models;

namespace OpenWorker.Content.Tool.ViewModels;

/// <summary>
/// Shared list of district or maze batch maps with a 3D view of the selected file's boxes.
/// </summary>
public abstract partial class BatchMapsViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty]
    public partial string Filter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsIdle { get; set; } = true;

    [ObservableProperty]
    public partial BatchMapEntry? SelectedMap { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<BatchBoxDraw> Boxes { get; set; } = [];

    [ObservableProperty]
    public partial string SelectionStatus { get; set; } = "Select a map to draw its boxes.";

    public ObservableCollection<BatchMapEntry> Maps { get; } = [];

    private IReadOnlyList<BatchMapEntry> _all = [];
    private CancellationTokenSource? _loadCts;
    private int _loadVersion;
    private bool _loaded;
    private CancellationTokenSource? _boxesCts;

    protected abstract string PanelTitle { get; }

    protected abstract IReadOnlyList<BatchMapEntry> LoadEntries(string resFolder, string? batchFolder, out string status);

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || IsLoading)
        {
            return;
        }

        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var version = Interlocked.Increment(ref _loadVersion);

        Maps.Clear();
        SelectedMap = null;
        Boxes = [];
        _all = [];
        _loaded = false;
        Title = PanelTitle;
        SelectionStatus = "Select a map to draw its boxes.";

        var res = ContentPaths.ResFolder;
        if (res is null)
        {
            Status = $"Set {ContentPaths.ResVariable} to the folder with the client *.res tables.";
            SetLoading(false);
            return;
        }

        var batch = ContentPaths.BatchFolder;
        SetLoading(true);
        Status = $"Loading {PanelTitle.ToLowerInvariant()}…";

        try
        {
            var loaded = await Task.Run(
                () =>
                {
                    token.ThrowIfCancellationRequested();
                    var entries = LoadEntries(res, batch, out var status);
                    token.ThrowIfCancellationRequested();
                    return (Entries: entries, Status: status);
                },
                token);

            if (version != _loadVersion)
            {
                return;
            }

            _all = loaded.Entries;
            _loaded = true;
            Status = batch is null
                ? $"{loaded.Status}. Set {ContentPaths.BatchVariable} to the folder with *.vbatch files."
                : loaded.Status;
            ApplyFilter();
        }
        catch (OperationCanceledException)
        {
            if (version == _loadVersion)
            {
                Status = "Loading cancelled.";
            }
        }
        catch (Exception exception)
        {
            if (version == _loadVersion)
            {
                Status = string.Format(
                    CultureInfo.InvariantCulture,
                    "Failed to load {0}: {1}",
                    PanelTitle.ToLowerInvariant(),
                    exception.Message);
            }
        }
        finally
        {
            if (version == _loadVersion)
            {
                SetLoading(false);
            }
        }
    }

    partial void OnFilterChanged(string value)
    {
        if (IsLoading)
        {
            return;
        }

        ApplyFilter();
    }

    partial void OnSelectedMapChanged(BatchMapEntry? value) => _ = ShowBoxesAsync(value);

    private async Task ShowBoxesAsync(BatchMapEntry? entry)
    {
        _boxesCts?.Cancel();
        _boxesCts?.Dispose();
        _boxesCts = new CancellationTokenSource();
        var token = _boxesCts.Token;

        Boxes = [];

        if (entry is null)
        {
            SelectionStatus = "Select a map to draw its boxes.";
            return;
        }

        if (!entry.FileExists)
        {
            SelectionStatus = string.Format(
                CultureInfo.InvariantCulture,
                "Batch file missing: {0}.vbatch",
                entry.BatchName);
            return;
        }

        SelectionStatus = string.Format(
            CultureInfo.InvariantCulture,
            "Loading {0}.vbatch…",
            entry.BatchName);

        try
        {
            var boxes = await Task.Run(
                () =>
                {
                    token.ThrowIfCancellationRequested();
                    return BatchMapCatalog.LoadBoxes(entry.Path);
                },
                token);

            if (token.IsCancellationRequested || !ReferenceEquals(SelectedMap, entry))
            {
                return;
            }

            Boxes = boxes;
            SelectionStatus = string.Format(
                CultureInfo.InvariantCulture,
                "{0}: {1} boxes",
                entry.BatchName,
                boxes.Count);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (!token.IsCancellationRequested && ReferenceEquals(SelectedMap, entry))
            {
                SelectionStatus = string.Format(
                    CultureInfo.InvariantCulture,
                    "Failed to read {0}: {1}",
                    entry.BatchName,
                    exception.Message);
            }
        }
    }

    private void ApplyFilter()
    {
        var filter = Filter.Trim();
        IEnumerable<BatchMapEntry> query = _all;
        if (filter.Length > 0)
        {
            query = _all.Where(entry =>
                entry.BatchName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                entry.Id.ToString(CultureInfo.InvariantCulture).Contains(filter, StringComparison.Ordinal));
        }

        Maps.Clear();
        foreach (var entry in query)
        {
            Maps.Add(entry);
        }

        if (SelectedMap is not null && !Maps.Contains(SelectedMap))
        {
            SelectedMap = Maps.Count > 0 ? Maps[0] : null;
        }
        else if (SelectedMap is null && Maps.Count > 0)
        {
            SelectedMap = Maps[0];
        }
    }

    private void SetLoading(bool loading)
    {
        IsLoading = loading;
        IsIdle = !loading;
    }

    public void Dispose()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        _boxesCts?.Cancel();
        _boxesCts?.Dispose();
        _boxesCts = null;
        GC.SuppressFinalize(this);
    }
}

public sealed class DistrictsViewModel : BatchMapsViewModel
{
    protected override string PanelTitle => "Districts";

    protected override IReadOnlyList<BatchMapEntry> LoadEntries(string resFolder, string? batchFolder, out string status) =>
        BatchMapCatalog.LoadDistricts(resFolder, batchFolder, out status);
}

public sealed class MazesViewModel : BatchMapsViewModel
{
    protected override string PanelTitle => "Mazes";

    protected override IReadOnlyList<BatchMapEntry> LoadEntries(string resFolder, string? batchFolder, out string status) =>
        BatchMapCatalog.LoadMazes(resFolder, batchFolder, out status);
}
