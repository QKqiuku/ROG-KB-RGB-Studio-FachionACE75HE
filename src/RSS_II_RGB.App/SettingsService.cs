using System.Text.Json;

namespace RSS_II_RGB.App;

/// <summary>Loads and saves <see cref="AppSettings"/> to settings.json under %LOCALAPPDATA%\RSS_II_RGB.</summary>
internal sealed class SettingsService
{
    private readonly string _path;
    private readonly SemaphoreSlim _saveGate=new(1,1);

    public AppSettings Settings { get; }
    internal string UserPresetAssetDirectory=>Path.Combine(Path.GetDirectoryName(_path)!,"user-presets");

    public SettingsService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppConstants.AppFolderName, Program.EnableFalchion75 ? "settings-falchion75.json" : "settings.json"))
    {
    }

    internal SettingsService(string path)
    {
        _path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        Settings = Load();
    }

    public void Save()
    {
        _saveGate.Wait();
        string temporary=_path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            File.WriteAllText(temporary,JsonSerializer.Serialize(Settings,AppSettingsJsonContext.Default.AppSettings));
            File.Move(temporary,_path,true);
        }
        catch { }
        finally {Cleanup(temporary);_saveGate.Release();}
    }
    internal async Task<bool> SaveAsync()
    {
        await _saveGate.WaitAsync().ConfigureAwait(false);
        string temporary=_path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            string json=JsonSerializer.Serialize(Settings,AppSettingsJsonContext.Default.AppSettings);
            await File.WriteAllTextAsync(temporary,json).ConfigureAwait(false);
            File.Move(temporary,_path,true);
            return true;
        }
        catch {return false;}
        finally {Cleanup(temporary);_saveGate.Release();}
    }
    private static void Cleanup(string temporary) {try {File.Delete(temporary);} catch {}}

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                string json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings) ?? new AppSettings();
            }
        }
        catch
        {
            // Corrupt/unreadable settings fall back to defaults.
        }
        return new AppSettings();
    }
}
