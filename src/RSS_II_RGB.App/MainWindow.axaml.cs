using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RSS_II_RGB.App;

public partial class MainWindow : Window
{
    private readonly Avalonia.Threading.DispatcherTimer _connectionMonitorTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    public MainWindow()
    {
        InitializeComponent();
        WorkbenchInput.Attach(this);
        _connectionMonitorTimer.Tick += (_, _) => (DataContext as MainViewModel)?.RefreshConnectionDiagnostics();
        Closed += (_, _) => _connectionMonitorTimer.Stop();
        if(Screens.Primary is { } screen) Width=Math.Max(MinWidth,Math.Min(Width,screen.WorkingArea.Width/screen.Scaling));
    }

    private async void OnConnectionMonitorOpened(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        vm.RefreshConnectionDiagnostics();
        _connectionMonitorTimer.Start();
        await vm.CheckLightingControllersAsync();
    }

    private void OnConnectionMonitorClosed(object? sender, EventArgs e) => _connectionMonitorTimer.Stop();
    private void OnReconnect(object? sender, RoutedEventArgs e) => (DataContext as MainViewModel)?.RetryConnection();
    private async void OnCheckLightingControllers(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) await vm.CheckLightingControllersAsync();
    }
    private async void OnCopyConnectionDiagnostics(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        vm.RefreshConnectionDiagnostics();
        try
        {
            var clipboard = GetTopLevel(this)?.Clipboard;
            if (clipboard is null) throw new InvalidOperationException("Clipboard unavailable.");
            await clipboard.SetTextAsync(vm.ConnectionDiagnostics);
            ConnectionCopyStatus.Text = L.ConnectionCopied;
        }
        catch { ConnectionCopyStatus.Text = L.ConnectionCopyFailed; }
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
