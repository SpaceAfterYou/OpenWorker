using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenWorker.Content.Tool.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Craftables = new CraftablesViewModel();
        ShowViewport();
    }

    public CraftablesViewModel Craftables { get; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    [ObservableProperty]
    public partial string SelectedPanel { get; set; } = "Viewport";

    [ObservableProperty]
    public partial bool IsViewportVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsCraftablesVisible { get; set; }

    [RelayCommand]
    public void ShowViewport()
    {
        SelectedPanel = "Viewport";
        IsViewportVisible = true;
        IsCraftablesVisible = false;
        StatusText = "Viewport — grid and RGB axes (Silk.NET smoke).";
    }

    [RelayCommand]
    public async Task ShowCraftablesAsync()
    {
        SelectedPanel = "Craftables";
        IsViewportVisible = false;
        IsCraftablesVisible = true;
        StatusText = "Loading craftables…";
        await Craftables.LoadAsync();
        StatusText = Craftables.Status;
    }
}
