using CommunityToolkit.Mvvm.ComponentModel;
using RSS_II_RGB.Core.Device;

namespace RSS_II_RGB.App;

internal sealed partial class MainViewModel
{
    private readonly ILightingControllerMonitor _lightingMonitor = new RSS_II_RGB.Windows.Win32LightingControllerMonitor();
    private string _runningControllers = L.ConnectionNotChecked;
    [ObservableProperty] private string _connectionDiagnostics = "";

    internal static string ConnectionFailureText(Exception? error) => error switch
    {
        KeyboardConnectionException { Issue: KeyboardConnectionIssue.SoftwareModeConflict } => L.ConnectionConflict,
        KeyboardConnectionException { Issue: KeyboardConnectionIssue.Unavailable } => L.ConnectionUnavailable,
        null => L.StatusSearching,
        _ => L.ConnectionFailed,
    };

    public void RefreshConnectionDiagnostics()
    {
        string Time(DateTimeOffset? value) => value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? L.ConnectionNone;
        ConnectionDiagnostics = $"{AppConstants.ProductName}\n{DeviceName}\n" +
            $"{L.ConnectionStateLabel}: {(_controller.IsConnected ? L.ConnectionStreaming : ConnectionFailureText(_controller.ConnectionError))}\n" +
            $"{L.ConnectionFirmwareLabel}: {(_controller.LastKnownFirmware.Length > 0 ? _controller.LastKnownFirmware : L.ConnectionNone)}\n" +
            $"{L.ConnectionAttemptLabel}: {Time(_controller.LastConnectionAttemptAt)}\n" +
            $"{L.ConnectionFrameLabel}: {Time(_controller.LastSuccessfulFrameAt)}\n" +
            $"{L.ConnectionFramesLabel}: {_controller.FramesRendered}\n" +
            $"{L.ConnectionErrorLabel}: {_controller.ConnectionError?.GetBaseException().Message ?? L.ConnectionNone}\n\n" +
            $"{L.ConnectionProcessesLabel}: {_runningControllers}\n{L.ConnectionProcessesHint}\n\n{L.ConnectionRecovery}";
    }

    public async Task CheckLightingControllersAsync()
    {
        try
        {
            var names = await Task.Run(_lightingMonitor.GetRunningControllers);
            _runningControllers = names.Count == 0 ? L.ConnectionNone : string.Join(", ", names);
        }
        catch { _runningControllers = L.ConnectionProcessCheckFailed; }
        RefreshConnectionDiagnostics();
    }

    public void RetryConnection()
    {
        _controller.RequestReconnect();
        RefreshConnectionDiagnostics();
    }
}
