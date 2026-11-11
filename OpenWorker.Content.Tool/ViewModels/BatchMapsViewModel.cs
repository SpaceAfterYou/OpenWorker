using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenWorker.Content.Tool.Data;
using OpenWorker.Content.Tool.Models;
using OpenWorker.Navigation;

namespace OpenWorker.Content.Tool.ViewModels;

/// <summary>
/// Shared list of district or maze batch maps with a 3D view of boxes and the matching navigation mesh.
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
    public partial IReadOnlyList<BatchBoxGroup> BoxGroups { get; set; } = [];

    [ObservableProperty]
    public partial object? SelectedBoxNode { get; set; }

    [ObservableProperty]
    public partial BatchBoxDraw? SelectedBox { get; set; }

    [ObservableProperty]
    public partial NavMesh? NavMesh { get; set; }

    [ObservableProperty]
    public partial SceneMeshDraw? SceneMesh { get; set; }

    [ObservableProperty]
    public partial bool ShowBoxesLayer { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowNavMeshLayer { get; set; }

    [ObservableProperty]
    public partial bool HasNavMesh { get; set; }

    [ObservableProperty]
    public partial int SelectedLayerTab { get; set; }

    [ObservableProperty]
    public partial string NavMeshStatus { get; set; } = "No navigation mesh for this map.";

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
        BoxGroups = [];
        SelectedBoxNode = null;
        SelectedBox = null;
        NavMesh = null;
        SceneMesh = null;
        HasNavMesh = false;
        NavMeshStatus = "No navigation mesh for this map.";
        SelectedLayerTab = 0;
        ApplyLayerTab(0);
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
            var nav = ContentPaths.NavMeshFolder;
            var world = ContentPaths.WorldFolder;
            var parts = new List<string> { loaded.Status };
            if (batch is null)
            {
                parts.Add($"Set {ContentPaths.BatchVariable} to the folder with *.vbatch files.");
            }

            if (nav is null)
            {
                parts.Add($"Set {ContentPaths.NavMeshVariable} to the folder with *.hkt meshes.");
            }

            if (world is null)
            {
                parts.Add($"Set {ContentPaths.WorldVariable} to the unpacked client datas (World/Scenes).");
            }

            Status = string.Join(' ', parts);
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

    partial void OnSelectedMapChanged(BatchMapEntry? value) => _ = ShowMapAsync(value);

    partial void OnSelectedBoxNodeChanged(object? value)
    {
        SelectedBox = value as BatchBoxDraw;
    }

    partial void OnSelectedLayerTabChanged(int value) => ApplyLayerTab(value);

    private void ApplyLayerTab(int tab)
    {
        ShowBoxesLayer = tab == 0;
        ShowNavMeshLayer = tab == 1;
        if (tab != 0)
        {
            SelectedBoxNode = null;
            SelectedBox = null;
        }
    }

    private async Task ShowMapAsync(BatchMapEntry? entry)
    {
        _boxesCts?.Cancel();
        _boxesCts?.Dispose();
        _boxesCts = new CancellationTokenSource();
        var token = _boxesCts.Token;

        Boxes = [];
        BoxGroups = [];
        SelectedBoxNode = null;
        SelectedBox = null;
        NavMesh = null;
        SceneMesh = null;
        HasNavMesh = false;
        NavMeshStatus = "No navigation mesh for this map.";

        if (entry is null)
        {
            SelectionStatus = "Select a map to draw its boxes.";
            return;
        }

        var navFolder = ContentPaths.NavMeshFolder;
        var navPath = navFolder is null
            ? null
            : Path.Combine(navFolder, entry.BatchName + ".hkt");
        var navExists = navPath is not null && File.Exists(navPath);

        var worldFolder = ContentPaths.WorldFolder;
        var scenePath = worldFolder is null
            ? null
            : SceneCatalog.FindScenePath(worldFolder, entry.BatchName);
        var sceneExists = scenePath is not null;

        if (!entry.FileExists && !navExists && !sceneExists)
        {
            SelectionStatus = string.Format(
                CultureInfo.InvariantCulture,
                "Missing {0}.vbatch / {0}.hkt / {0}.vscene",
                entry.BatchName);
            return;
        }

        SelectionStatus = string.Format(
            CultureInfo.InvariantCulture,
            "Loading {0}…",
            entry.BatchName);

        try
        {
            var loaded = await Task.Run(
                () =>
                {
                    token.ThrowIfCancellationRequested();

                    IReadOnlyList<BatchBoxDraw> boxes = [];
                    string? boxError = null;
                    if (entry.FileExists)
                    {
                        try
                        {
                            boxes = BatchMapCatalog.LoadBoxes(entry.Path);
                        }
                        catch (Exception exception)
                        {
                            boxError = exception.Message;
                        }
                    }

                    token.ThrowIfCancellationRequested();

                    NavMesh? mesh = null;
                    string? navError = null;
                    if (navExists)
                    {
                        try
                        {
                            mesh = NavMeshFileCache.Shared.Load(navPath!);
                        }
                        catch (Exception exception)
                        {
                            navError = exception.Message;
                        }
                    }

                    token.ThrowIfCancellationRequested();

                    SceneMeshDraw? scene = null;
                    string? sceneError = null;
                    if (sceneExists && worldFolder is not null)
                    {
                        try
                        {
                            scene = SceneFileCache.Shared.Load(worldFolder, scenePath!);
                        }
                        catch (Exception exception)
                        {
                            sceneError = exception.Message;
                        }
                    }

                    return (Boxes: boxes, BoxError: boxError, Mesh: mesh, NavError: navError, Scene: scene, SceneError: sceneError);
                },
                token);

            if (token.IsCancellationRequested || !ReferenceEquals(SelectedMap, entry))
            {
                return;
            }

            Boxes = loaded.Boxes;
            BoxGroups = BatchMapCatalog.GroupBoxes(loaded.Boxes);
            NavMesh = loaded.Mesh;
            SceneMesh = loaded.Scene;
            HasNavMesh = loaded.Mesh is not null;
            NavMeshStatus = FormatNavMeshStatus(entry.BatchName, loaded.Mesh, loaded.NavError, navExists);

            SelectionStatus = FormatSelectionStatus(
                entry.BatchName,
                loaded.Boxes.Count,
                loaded.Mesh,
                loaded.Scene,
                loaded.BoxError,
                loaded.NavError,
                loaded.SceneError,
                entry.FileExists,
                navExists,
                sceneExists,
                worldFolder is not null);
            ApplyLayerTab(SelectedLayerTab);
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

    private static string FormatSelectionStatus(
        string name,
        int boxCount,
        NavMesh? mesh,
        SceneMeshDraw? scene,
        string? boxError,
        string? navError,
        string? sceneError,
        bool batchExists,
        bool navExists,
        bool sceneExists,
        bool worldConfigured)
    {
        var parts = new List<string>(5);

        if (boxError is not null)
        {
            parts.Add("batch: " + boxError);
        }
        else if (batchExists)
        {
            parts.Add(string.Format(CultureInfo.InvariantCulture, "{0} boxes", boxCount));
        }
        else
        {
            parts.Add("batch missing");
        }

        if (navError is not null)
        {
            parts.Add("navmesh: " + navError);
        }
        else if (mesh is not null)
        {
            parts.Add(string.Format(
                CultureInfo.InvariantCulture,
                "navmesh {0} faces",
                mesh.FaceCount));
        }
        else if (!navExists)
        {
            parts.Add("navmesh missing");
        }

        if (sceneError is not null)
        {
            parts.Add("scene: " + sceneError);
        }
        else if (scene is { TriangleCount: > 0 })
        {
            parts.Add(string.Format(
                CultureInfo.InvariantCulture,
                "scene {0} tris ({1} meshes)",
                scene.TriangleCount,
                scene.MeshCount));
        }
        else if (!worldConfigured)
        {
            parts.Add("scene unset");
        }
        else if (!sceneExists)
        {
            parts.Add("scene missing");
        }
        else
        {
            parts.Add("scene empty");
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}: {1}",
            name,
            string.Join(", ", parts));
    }

    private static string FormatNavMeshStatus(string name, NavMesh? mesh, string? navError, bool navExists)
    {
        if (navError is not null)
        {
            return string.Format(CultureInfo.InvariantCulture, "Failed to read {0}.hkt: {1}", name, navError);
        }

        if (mesh is not null)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}.hkt — {1} faces, {2} edges, {3} vertices",
                name,
                mesh.FaceCount,
                mesh.EdgeCount,
                mesh.VertexCount);
        }

        if (!navExists)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.hkt is missing.", name);
        }

        return "No navigation mesh for this map.";
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
