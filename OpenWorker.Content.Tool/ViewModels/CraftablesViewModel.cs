using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenWorker.Content.Tool.Data;
using OpenWorker.Content.Tool.Models;

namespace OpenWorker.Content.Tool.ViewModels;

public partial class CraftablesViewModel : ViewModelBase, IDisposable
{
    public const int PageSize = 40;

    [ObservableProperty]
    public partial string Filter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial string PageLabel { get; set; } = "Page 1 / 1";

    [ObservableProperty]
    public partial bool CanGoPrevious { get; set; }

    [ObservableProperty]
    public partial bool CanGoNext { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsIdle { get; set; } = true;

    [ObservableProperty]
    public partial CraftableItemEntry? SelectedItem { get; set; }

    public ObservableCollection<CraftableItemEntry> Items { get; } = [];

    private IReadOnlyList<CraftableItemEntry> _all = [];
    private IReadOnlyList<CraftableItemEntry> _filtered = [];
    private CancellationTokenSource? _loadCts;
    private int _loadVersion;
    private bool _loaded;

    /// <summary>
    /// Loads the craftables once; later calls keep what is already in memory.
    /// </summary>
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

        Items.Clear();
        SelectedItem = null;
        _all = [];
        _filtered = [];
        _loaded = false;
        CurrentPage = 1;
        RefreshPageState();

        var res = ContentPaths.ResFolder;
        if (res is null)
        {
            Status = $"Set {ContentPaths.ResVariable} to the folder with the client *.res tables.";
            SetLoading(false);
            return;
        }

        var gui = ContentPaths.GuiFolder;
        SetLoading(true);
        Status = "Loading craftables…";

        try
        {
            var loaded = await Task.Run(
                () =>
                {
                    token.ThrowIfCancellationRequested();
                    var entries = CraftableCatalog.Load(res, gui, out var status);
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
            Status = gui is null
                ? $"{loaded.Status}. Set {ContentPaths.GuiVariable} to the GUI root (folder with Icon/Item)."
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
                    "Failed to load craftables: {0}",
                    exception.Message);
                RefreshPageState();
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

        CurrentPage = 1;
        ApplyFilter();
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void PreviousPage()
    {
        if (CurrentPage <= 1)
        {
            return;
        }

        CurrentPage--;
        FillPage();
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void NextPage()
    {
        if (CurrentPage >= TotalPages)
        {
            return;
        }

        CurrentPage++;
        FillPage();
    }

    private void SetLoading(bool loading)
    {
        IsLoading = loading;
        IsIdle = !loading;
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    private void ApplyFilter()
    {
        var filter = Filter.Trim();
        if (filter.Length == 0)
        {
            _filtered = _all;
        }
        else
        {
            _filtered = _all
                .Where(item =>
                    item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    item.ItemId.ToString(CultureInfo.InvariantCulture).Contains(filter, StringComparison.Ordinal) ||
                    item.PrimaryPath.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        TotalPages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
        }

        FillPage();
    }

    private void FillPage()
    {
        Items.Clear();
        var start = (CurrentPage - 1) * PageSize;

        for (var i = start; i < _filtered.Count && i < start + PageSize; i++)
        {
            Items.Add(_filtered[i]);
        }

        if (SelectedItem is not null && !Items.Contains(SelectedItem))
        {
            SelectedItem = Items.Count > 0 ? Items[0] : null;
        }
        else if (SelectedItem is null && Items.Count > 0)
        {
            SelectedItem = Items[0];
        }

        RefreshPageState();
    }

    private void RefreshPageState()
    {
        PageLabel = string.Format(
            CultureInfo.InvariantCulture,
            "Page {0} / {1} · {2} shown · {3} total",
            CurrentPage,
            TotalPages,
            Items.Count,
            _filtered.Count);
        CanGoPrevious = !IsLoading && CurrentPage > 1;
        CanGoNext = !IsLoading && CurrentPage < TotalPages;
    }

    public void Dispose()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        GC.SuppressFinalize(this);
    }
}
