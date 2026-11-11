using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenWorker.Content.Tool.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Craftables = new CraftablesViewModel();
        Items = new ItemsViewModel();
        Districts = new DistrictsViewModel();
        Mazes = new MazesViewModel();
        ShowViewport();
    }

    public CraftablesViewModel Craftables { get; }

    public ItemsViewModel Items { get; }

    public DistrictsViewModel Districts { get; }

    public MazesViewModel Mazes { get; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    [ObservableProperty]
    public partial string SelectedPanel { get; set; } = "Viewport";

    [ObservableProperty]
    public partial bool IsViewportVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsCraftablesVisible { get; set; }

    [ObservableProperty]
    public partial bool IsItemsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsDistrictsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsMazesVisible { get; set; }

    private void HideAll()
    {
        IsViewportVisible = false;
        IsCraftablesVisible = false;
        IsItemsVisible = false;
        IsDistrictsVisible = false;
        IsMazesVisible = false;
    }

    [RelayCommand]
    public void ShowViewport()
    {
        SelectedPanel = "Viewport";
        HideAll();
        IsViewportVisible = true;
        StatusText = "Viewport — grid and RGB axes (Silk.NET smoke).";
    }

    [RelayCommand]
    public async Task ShowCraftablesAsync()
    {
        SelectedPanel = "Craftables";
        HideAll();
        IsCraftablesVisible = true;
        StatusText = string.IsNullOrEmpty(Craftables.Status) ? "Loading craftables…" : Craftables.Status;
        await Craftables.EnsureLoadedAsync();
        StatusText = Craftables.Status;
    }

    [RelayCommand]
    public async Task ShowItemsAsync()
    {
        SelectedPanel = "Items";
        HideAll();
        IsItemsVisible = true;
        StatusText = string.IsNullOrEmpty(Items.Status) ? "Loading items…" : Items.Status;
        await Items.EnsureLoadedAsync();
        StatusText = Items.Status;
    }

    [RelayCommand]
    public async Task ShowDistrictsAsync()
    {
        SelectedPanel = "Districts";
        HideAll();
        IsDistrictsVisible = true;
        StatusText = string.IsNullOrEmpty(Districts.Status) ? "Loading districts…" : Districts.Status;
        await Districts.EnsureLoadedAsync();
        StatusText = Districts.Status;
    }

    [RelayCommand]
    public async Task ShowMazesAsync()
    {
        SelectedPanel = "Mazes";
        HideAll();
        IsMazesVisible = true;
        StatusText = string.IsNullOrEmpty(Mazes.Status) ? "Loading mazes…" : Mazes.Status;
        await Mazes.EnsureLoadedAsync();
        StatusText = Mazes.Status;
    }
}
