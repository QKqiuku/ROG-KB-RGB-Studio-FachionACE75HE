using RSS_II_RGB.App;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;

namespace ROG_KB_RGB_Studio.App.Tests;

public class PresetLibraryTests
{
    [Fact]
    public async Task FavoritesHistoryAndTransformsSurviveReloadWithoutChangingZonesOrFrameRate()
    {
        string folder = Path.Combine(Path.GetTempPath(), "RSS_II_RGB-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "settings.json");
        await using var controller = new KeyboardController(new QuietLog(), new SensorState());
        try
        {
            var settings = new SettingsService(path);
            settings.Settings.TargetFps = 30;
            settings.Settings.Zones = [new ZoneSetting { KeyIndices = [0], ColorHex = "FF0000" }];
            var vm = new MainViewModel(controller, settings, new Startup()); vm.RestoreSettings();
            vm.RegularPresets.Single(p => p.Effect == EffectChoice.Aurora).SelectCommand.Execute(null);
            vm.ToggleFavoriteCommand.Execute(null);
            vm.MirrorX = true; vm.MirrorY = true; vm.RotatePatternCommand.Execute(null);
            var loaded = new MainViewModel(controller, new SettingsService(path), new Startup()); loaded.RestoreSettings();
            Assert.Contains(loaded.FavoritePresets, p => p.Effect == EffectChoice.Aurora);
            Assert.Equal(EffectChoice.Aurora, loaded.RecentPresets[0].Effect);
            Assert.True(loaded.MirrorX); Assert.True(loaded.MirrorY); Assert.Equal(90, loaded.EffectDirection);
            Assert.Equal(30, loaded.TargetFps); Assert.Single(new SettingsService(path).Settings.Zones);
            for (int i = 0; i < 15; i++) loaded.RandomPresetCommand.Execute(null);
            Assert.InRange(loaded.RecentPresets.Length, 1, 8);
            Assert.DoesNotContain(loaded.RecentPresets, p => p.Effect is EffectChoice.Gif or EffectChoice.Atk);
        }
        finally { foreach (string file in Directory.EnumerateFiles(folder)) File.Delete(file); Directory.Delete(folder); }
    }
    [Theory]
    [InlineData((int)EffectChoice.KeyHeart)]
    [InlineData((int)EffectChoice.KeyDoubleRing)]
    [InlineData((int)EffectChoice.Heart)]
    [InlineData((int)EffectChoice.HeartRipple)]
    [InlineData((int)EffectChoice.Plasma)]
    [InlineData((int)EffectChoice.NeonBorder)]
    [InlineData((int)EffectChoice.Fireflies)]
    [InlineData((int)EffectChoice.Galaxy)]
    [InlineData((int)EffectChoice.Tunnel)]
    [InlineData((int)EffectChoice.Kaleidoscope)]
    [InlineData((int)EffectChoice.Bubbles)]
    [InlineData((int)EffectChoice.Diamonds)]
    [InlineData((int)EffectChoice.Monet)]
    [InlineData((int)EffectChoice.Rain)]
    [InlineData((int)EffectChoice.Waterfall)]
    [InlineData((int)EffectChoice.Bloom)]
    public async Task RemovedEffectsCannotReturnThroughSavedStateOrLibrary(int removedValue)
    {
        var removed=(EffectChoice)removedValue;
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var settings=new SettingsService(path);
            settings.Settings.GlobalEffect=removed;
            settings.Settings.CustomBaseEffect=removed;
            settings.Settings.FavoriteEffects=[removed,EffectChoice.Ocean];
            settings.Settings.RecentEffects=[removed,EffectChoice.Ocean];
            settings.Settings.TargetFps=30;
            settings.Settings.Zones=[new ZoneSetting {KeyIndices=[0],ColorHex="FF0000"}];
            settings.Save();
            var reloaded=new SettingsService(path);
            var vm=new MainViewModel(controller,reloaded,new Startup()); vm.RestoreSettings();
            Assert.DoesNotContain(removed,PresetCatalog.All);
            Assert.Null(PresetCatalog.PatternFor(removed));
            Assert.Equal(EffectChoice.Rainbow,vm.SelectedEffect);
            Assert.Equal(EffectChoice.Rainbow,reloaded.Settings.CustomBaseEffect);
            Assert.DoesNotContain(removed,reloaded.Settings.FavoriteEffects);
            Assert.DoesNotContain(removed,reloaded.Settings.RecentEffects);
            Assert.Contains(EffectChoice.Ocean,reloaded.Settings.FavoriteEffects);
            Assert.Equal(30,vm.TargetFps); Assert.Single(reloaded.Settings.Zones);
            vm.IsCustomMode=false; vm.IsCustomMode=true;
            Assert.Equal(EffectChoice.Rainbow,vm.SelectedEffect);
        }
        finally {File.Delete(path);}
    }
    private sealed class QuietLog : ILogSink { public void Log(LogLevel level, string message, Exception? exception = null) { } }
    private sealed class Startup : IStartupManager { public bool IsSupported => false; public bool IsEnabled() => false; public void SetEnabled(bool enabled) { } }
}
