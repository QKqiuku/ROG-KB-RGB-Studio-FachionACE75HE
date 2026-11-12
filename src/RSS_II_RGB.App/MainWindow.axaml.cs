using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RSS_II_RGB.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WorkbenchInput.Attach(this);
        if(Screens.Primary is { } screen) Width=Math.Max(MinWidth,Math.Min(Width,screen.WorkingArea.Width/screen.Scaling));
    }

    private void OnEditCustomLayers(object? sender, RoutedEventArgs e) => ZoneHost.BringIntoView();

    private async void OnExit(object? sender, RoutedEventArgs e)
    {
        if (Avalonia.Application.Current is App app) await app.ExitAsync();
    }

    private async void OnImportGif(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.ImportGifAsync(this);
        }
    }

    private async void OnOpenAtkGallery(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await new AtkGalleryWindow { DataContext = new AtkGalleryViewModel(vm) }.ShowDialog(this);
    }
}
