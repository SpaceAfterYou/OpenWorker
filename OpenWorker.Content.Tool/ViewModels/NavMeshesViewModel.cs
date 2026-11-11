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
    public partial SceneMeshDraw? SceneMesh { get; set; }

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
        SceneMesh = null;
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
            var parts = new List<string> { loaded.Status };
            if (ContentPaths.WorldFolder is null)
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
        SceneMesh = null;

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
            "Loading {0}…",
            entry.Name);

        var worldFolder = ContentPaths.WorldFolder;
        var scenePath = worldFolder is null
            ? null
            : SceneCatalog.FindScenePath(worldFolder, entry.Name);
        var canLoadScene = scenePath is not null && worldFolder is not null;

        try
        {
            var loaded = await Task.Run(
                () =>
                {
                    token.ThrowIfCancellationRequested();
                    var mesh = NavMeshFileCache.Shared.Load(entry.Path);

                    token.ThrowIfCancellationRequested();
                    SceneMeshDraw? scene = null;
                    string? sceneError = null;
                    if (canLoadScene)
                    {
                        try
                        {
                            scene = SceneFileCache.Shared.Load(worldFolder!, scenePath!);
                        }
                        catch (Exception exception)
                        {
                            sceneError = exception.Message;
                        }
                    }
                    else if (worldFolder is null)
                    {
                        sceneError = string.Format(
                            CultureInfo.InvariantCulture,
                            "set {0}",
                            ContentPaths.WorldVariable);
                    }
                    else if (scenePath is null)
                    {
                        sceneError = "scene missing";
                    }

                    return (Mesh: mesh, Scene: scene, SceneError: sceneError);
                },
                token);

            if (token.IsCancellationRequested || !ReferenceEquals(SelectedEntry, entry))
            {
                return;
            }

            Mesh = loaded.Mesh;
            SceneMesh = loaded.Scene;
            SelectionStatus = FormatSelectionStatus(entry, loaded.Mesh, loaded.Scene, loaded.SceneError);
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

    private static string FormatSelectionStatus(
        NavMeshEntry entry,
        NavMesh mesh,
        SceneMeshDraw? scene,
        string? sceneError)
    {
        var parts = new List<string>(2)
        {
            entry.FormatCounts(mesh.FaceCount, mesh.EdgeCount, mesh.VertexCount)
        };

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
        else if (scene is not null)
        {
            parts.Add("scene empty");
        }

        return string.Join(", ", parts);
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
