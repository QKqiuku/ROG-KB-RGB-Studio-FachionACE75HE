using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Windows;

namespace RSS_II_RGB.App;

public partial class App : Application
{
    private KeyboardController? _controller;
    private MainViewModel? _viewModel;
    private SensorService? _sensorService;
    private TrayIconManager? _tray;
    private ShutdownCleanup? _shutdown;
    private Task? _exitTask;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var log = new RotatingFileLogSink();
            var sensors = new SensorState();
            _sensorService = new SensorService(sensors, log);
            _controller = new KeyboardController(log, sensors);
            _shutdown = new ShutdownCleanup(_controller, _sensorService);
            var settings = new SettingsService();
            var startup = new Win32StartupManager(AppConstants.StartupRegistryValueName, AppConstants.StartMinimizedArg);
            var viewModel = new MainViewModel(_controller, settings, startup, new NamedPipeAudioControl());
            _viewModel=viewModel;

            var mainWindow = new MainWindow { DataContext = viewModel };
            desktop.MainWindow = mainWindow;
            desktop.ShutdownRequested += OnShutdownRequested;

            // Launched by auto-start? Come up hidden in the tray.
            bool startHidden = desktop.Args is { } args && args.Contains(AppConstants.StartMinimizedArg);

            // Keep running in the tray when the window is closed/minimised.
            _tray = new TrayIconManager(desktop, mainWindow, ExitAsync);
            _tray.Initialize(startHidden);

            _ = viewModel.InitializeAsync();
            _sensorService.Start();
            viewModel.StartAudioMonitoring();
        }

        base.OnFrameworkInitializationCompleted();
    }

    internal Task ExitAsync() => _exitTask ??= ExitCoreAsync();

    private async Task ExitCoreAsync()
    {
        _tray?.PrepareExit();
        _viewModel?.StopAudioMonitoring();
        try
        {
            if (_shutdown is not null) await _shutdown.StopAsync();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Shutdown cleanup failed: {ex}");
        }
        finally
        {
            _tray?.Dispose();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        // OS shutdown cannot await this event. Allow bounded restoration before
        // returning; force shutdown/power loss can still interrupt the device.
        _tray?.PrepareExit();
        _viewModel?.StopAudioMonitoring();
        try
        {
            if (_shutdown is not null && !_shutdown.StopAsync().Wait(AppConstants.OsShutdownGraceMs))
            {
                Trace.WriteLine("Shutdown cleanup exceeded the OS grace period.");
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Shutdown cleanup failed: {ex}");
        }
        _tray?.Dispose();
    }
}
