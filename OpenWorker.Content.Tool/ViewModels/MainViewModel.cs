using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenWorker.Content.Tool.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Craftables = new CraftablesViewModel();
        Items = new ItemsViewModel();
        ShowViewport();
    }

    public CraftablesViewModel Craftables { get; }

    public ItemsViewModel Items { get; }

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

    [RelayCommand]
    public void ShowViewport()
    {
        SelectedPanel = "Viewport";
        IsViewportVisible = true;
        IsCraftablesVisible = false;
        IsItemsVisible = false;
        StatusText = "Viewport — grid and RGB axes (Silk.NET smoke).";
    }

    [RelayCommand]
    public async Task ShowCraftablesAsync()
    {
        SelectedPanel = "Craftables";
        IsViewportVisible = false;
        IsCraftablesVisible = true;
        IsItemsVisible = false;
        StatusText = string.IsNullOrEmpty(Craftables.Status) ? "Loading craftables…" : Craftables.Status;
        await Craftables.EnsureLoadedAsync();
        StatusText = Craftables.Status;
    }

    [RelayCommand]
    public async Task ShowItemsAsync()
    {
        SelectedPanel = "Items";
        IsViewportVisible = false;
        IsCraftablesVisible = false;
        IsItemsVisible = true;
        StatusText = string.IsNullOrEmpty(Items.Status) ? "Loading items…" : Items.Status;
        await Items.EnsureLoadedAsync();
        StatusText = Items.Status;
    }
}
