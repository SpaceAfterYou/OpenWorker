using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenWorker.Content.Tool.Data;
using OpenWorker.Content.Tool.Models;
using OpenWorker.Navigation;

namespace OpenWorker.Content.Tool.ViewModels;

/// <summary>
/// Lists client <c>*.hkt</c> navigation meshes and shows the selected mesh in 3D.
/// </summary>
public sealed partial class NavMeshesViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty]
    public partial string Filter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsIdle { get; set; } = true;

    [ObservableProperty]
    public partial NavMeshEntry? SelectedEntry { get; set; }

    [ObservableProperty]
    public partial NavMesh? Mesh { get; set; }

    [ObservableProperty]
    public partial string SelectionStatus { get; set; } = "Select a navigation mesh.";

    public ObservableCollection<NavMeshEntry> Entries { get; } = [];

    private IReadOnlyList<NavMeshEntry> _all = [];
    private CancellationTokenSource? _loadCts;
    private int _loadVersion;
    private bool _loaded;
    private CancellationTokenSource? _meshCts;

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

        Entries.Clear();
        SelectedEntry = null;
        Mesh = null;
        _all = [];
        _loaded = false;
        SelectionStatus = "Select a navigation mesh.";

        var folder = ContentPaths.NavMeshFolder;
        SetLoading(true);
        Status = "Loading navigation meshes…";

        try
        {
            var loaded = await Task.Run(
                () =>
                {
                    token.ThrowIfCancellationRequested();
                    var entries = NavMeshCatalog.LoadEntries(folder, out var status);
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
            Status = loaded.Status;
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
                    "Failed to list navigation meshes: {0}",
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

    partial void OnSelectedEntryChanged(NavMeshEntry? value) => _ = ShowMeshAsync(value);

    private async Task ShowMeshAsync(NavMeshEntry? entry)
    {
        _meshCts?.Cancel();
        _meshCts?.Dispose();
        _meshCts = new CancellationTokenSource();
        var token = _meshCts.Token;

        Mesh = null;

        if (entry is null)
        {
            SelectionStatus = "Select a navigation mesh.";
            return;
        }

        if (!entry.FileExists)
        {
            SelectionStatus = string.Format(
                CultureInfo.InvariantCulture,
                "Navmesh file missing: {0}.hkt",
                entry.Name);
            return;
        }

        SelectionStatus = string.Format(
            CultureInfo.InvariantCulture,
            "Loading {0}.hkt…",
            entry.Name);

        try
        {
            var mesh = await Task.Run(
                () =>
                {
                    token.ThrowIfCancellationRequested();
                    return NavMeshFileCache.Shared.Load(entry.Path);
                },
                token);

            if (token.IsCancellationRequested || !ReferenceEquals(SelectedEntry, entry))
            {
                return;
            }

            Mesh = mesh;
            SelectionStatus = entry.FormatCounts(mesh.FaceCount, mesh.EdgeCount, mesh.VertexCount);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (!token.IsCancellationRequested && ReferenceEquals(SelectedEntry, entry))
            {
                SelectionStatus = string.Format(
                    CultureInfo.InvariantCulture,
                    "Failed to read {0}: {1}",
                    entry.Name,
                    exception.Message);
            }
        }
    }

    private void ApplyFilter()
    {
        var filter = Filter.Trim();
        IEnumerable<NavMeshEntry> query = _all;
        if (filter.Length > 0)
        {
            query = _all.Where(entry =>
                entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        Entries.Clear();
        foreach (var entry in query)
        {
            Entries.Add(entry);
        }

        if (SelectedEntry is not null && !Entries.Contains(SelectedEntry))
        {
            SelectedEntry = Entries.Count > 0 ? Entries[0] : null;
        }
        else if (SelectedEntry is null && Entries.Count > 0)
        {
            SelectedEntry = Entries[0];
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
        _meshCts?.Cancel();
        _meshCts?.Dispose();
        _meshCts = null;
        GC.SuppressFinalize(this);
    }
}
