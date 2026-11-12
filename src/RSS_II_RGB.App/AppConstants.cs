namespace RSS_II_RGB.App;

/// <summary>Centralised magic values for the App project (CLAUDE.md rule 8).</summary>
internal static class AppConstants
{
    public const string ProductName = "ROG KB RGB Studio";
    public const string AppFolderName = "RSS_II_RGB";                          // %LOCALAPPDATA%\RSS_II_RGB
    // Per-user single instance (no "Global\\" — one instance per logon session).
    public const string SingleInstanceMutexName = "RSS_II_RGB_SingleInstance_5b6c1f2a";
    // A second launch signals this event so the running instance surfaces from the tray.
    public const string ShowWindowEventName = "RSS_II_RGB_ShowWindow_5b6c1f2a";
    public static string AppTitle => L.WorkbenchTitle;

    // Auto-start: the HKCU\...\Run value name, and the argument that makes a
    // launch come up hidden in the tray (used by the auto-start registration).
    public const string StartupRegistryValueName = "RSS_II_RGB";
    public const string StartMinimizedArg = "--minimized";
    public const string ExperimentalFalchion75Arg = "--experimental-falchion75";
    public const int Falchion75TargetFps = 60; // Default target; completed-frame throughput depends on device/workload.
    public static readonly int[] FrameRateOptions = [15, 30, 45, 60];
    public static int NormalizeTargetFps(int value) => FrameRateOptions.MinBy(fps => Math.Abs((long)fps - value));
    public const string RogEyePreviewKeyName = "ROG eye";
    public const int GifHelperTimeoutMs = 60_000;
    public const int OsShutdownGraceMs = 5000;
    public const string AtkGalleryEndpoint = "https://api-hub.vxe.com/v1/share/config/list";
    public const string AtkGallerySource = "https://hub.atk.pro/";
    public const int AtkPageSize = 20;
    public const int AtkMaxCatalogueCharacters = 64 * 1024 * 1024; // Bound retained UTF-16 scene data.
    public const int AtkMaxResponseBytes = 8 * 1024 * 1024;
}
