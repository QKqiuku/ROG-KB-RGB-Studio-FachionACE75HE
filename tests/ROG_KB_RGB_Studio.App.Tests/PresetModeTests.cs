using RSS_II_RGB.App;
using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;

namespace ROG_KB_RGB_Studio.App.Tests;

public class PresetModeTests
{
    [Fact]
    public async Task ModeSwitchUsesCurrentPresetAndRemembersParametersPerPreset()
    {
        string folder = Path.Combine(Path.GetTempPath(), "RSS_II_RGB-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "settings.json");
        await using var controller = new KeyboardController(new QuietLog(), new SensorState());
        try
        {
            var store = new SettingsService(path);
            store.Settings.GlobalEffect = EffectChoice.Solid;
            store.Settings.GlobalColorHex = "0000FF";
            store.Settings.Zones = [new ZoneSetting { KeyIndices = [46], ColorHex = "FF0000" }];
            var vm = new MainViewModel(controller, store, new NoStartup());
            vm.RestoreSettings();
            Assert.True(vm.IsCustomMode);
            vm.TargetFps = 0;
            Assert.Equal(15, vm.TargetFps);
            vm.TargetFps = 999;
            Assert.Equal(60, vm.TargetFps);
            vm.TargetFps = 30;
            Assert.Equal(30, controller.TargetFps);
            Assert.Equal(30, new SettingsService(path).Settings.TargetFps);
            vm.PickedHex = null!;
            Assert.True(vm.InvalidColor);
            vm.RegularPresets.Single(p => p.Effect == EffectChoice.Wave).SelectCommand.Execute(null);
            Assert.False(vm.IsCustomMode);
            Assert.False(controller.EnableCustomLayers);
            vm.EffectSpeed = 2.4;
            vm.RegularPresets.Single(p => p.Effect == EffectChoice.SineWave).SelectCommand.Execute(null);
            vm.EffectSpeed = 0.5;
            vm.RegularPresets.Single(p => p.Effect == EffectChoice.Wave).SelectCommand.Execute(null);
            Assert.Equal(2.4, vm.EffectSpeed);
            vm.ChooseCustomCommand.Execute(null);
            Assert.True(vm.IsCustomMode);
            Assert.Equal(EffectChoice.Wave, vm.SelectedEffect);
            Assert.True(controller.EnableCustomLayers);
            Assert.Equal(30, vm.TargetFps);
            var saved = new SettingsService(path).Settings;
            Assert.True(saved.UseCustomLayers);
            Assert.Empty(saved.Zones);
            Assert.Equal(2.4, saved.EffectSettings[EffectChoice.Wave].Speed);
            Assert.Equal(0.5, saved.EffectSettings[EffectChoice.SineWave].Speed);
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(folder);
        }
    }
    [Fact]
    public async Task FrameRateChoicesMigrateLegacyValuesAndPersistLiveChanges()
    {
        string folder = Path.Combine(Path.GetTempPath(), "RSS_II_RGB-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "settings.json");
        await using var controller = new KeyboardController(new QuietLog(), new SensorState());
        try
        {
            foreach (var (legacy, expected) in new[] { (5, 15), (40, 45), (120, 60), (int.MinValue, 15), (int.MaxValue, 60) })
            {
                var store = new SettingsService(path);
                store.Settings.TargetFps = legacy;
                store.Settings.Zones = [new ZoneSetting { KeyIndices = [46], ColorHex = "FF0000" }];
                var vm = new MainViewModel(controller, store, new NoStartup());
                vm.RestoreSettings();
                Assert.Equal(expected, vm.TargetFps);
                Assert.Equal(expected, controller.TargetFps);
                Assert.Equal(expected, new SettingsService(path).Settings.TargetFps);
                foreach (int choice in new[] { 15, 30, 45, 60 })
                {
                    vm.TargetFps = choice;
                    Assert.Equal(choice, controller.TargetFps);
                    var saved = new SettingsService(path).Settings;
                    Assert.Equal(choice, saved.TargetFps);
                    Assert.Equal(new[] { 46 }, Assert.Single(saved.Zones).KeyIndices);
                }
            }
            controller.SetTargetFps(int.MaxValue);
            Assert.Equal(60, controller.TargetFps);
            controller.SetTargetFps(int.MinValue);
            Assert.Equal(15, controller.TargetFps);
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(folder);
        }
    }

    [Fact]
    public async Task PresetModeIgnoresCustomLayersWithoutDeletingTheirConfiguration()
    {
        await using var controller = new KeyboardController(new QuietLog(), new SensorState());
        int a = controller.Profile.IndexForKeyName("A");
        controller.SetGlobalEffect(EffectChoice.Solid, new Rgb(0, 0, 255), 1);
        controller.SetZones([new Zone([a], EffectChoice.Solid, new Rgb(255, 0, 0))]);
        controller.EnableStarlight = true;
        controller.EnableAudio = true;
        controller.EnableCustomLayers = false;
        Assert.All(Render(controller), p => Assert.Equal(new Rgb(0, 0, 255), p));
        Assert.Single(controller.Zones);
        Assert.True(controller.EnableStarlight);
        Assert.True(controller.EnableAudio);
        controller.EnableCustomLayers = true;
        Assert.Equal(new Rgb(255, 0, 0), Render(controller)[a]);
    }

    [Fact]
    public void LegacySettingsKeepCustomLayersAndEffectOptionsRoundTripIndependently()
    {
        string folder = Path.Combine(Path.GetTempPath(), "RSS_II_RGB-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "settings.json");
        try
        {
            File.WriteAllText(path, """{"GlobalEffect":"Wave","Zones":[{"KeyIndices":[46],"Effect":"Solid","ColorHex":"FF0000"}]}""");
            var settings = new SettingsService(path);
            Assert.True(settings.Settings.UseCustomLayers);
            settings.Settings.UseCustomLayers = false;
            settings.Settings.EffectSettings[EffectChoice.Wave] = new EffectSetting { Speed = 2, Width = 1.7, ColorHex = "123456" };
            settings.Settings.EffectSettings[EffectChoice.SineWave] = new EffectSetting { Speed = 0.5, Direction = 90 };
            settings.Save();
            var reloaded = new SettingsService(path).Settings;
            Assert.False(reloaded.UseCustomLayers);
            Assert.Single(reloaded.Zones);
            Assert.Equal(2, reloaded.EffectSettings[EffectChoice.Wave].Speed);
            Assert.Equal(1.7, reloaded.EffectSettings[EffectChoice.Wave].Width);
            Assert.Equal("123456", reloaded.EffectSettings[EffectChoice.Wave].ColorHex);
            Assert.Equal(0.5, reloaded.EffectSettings[EffectChoice.SineWave].Speed);
            Assert.Equal(90, reloaded.EffectSettings[EffectChoice.SineWave].Direction);
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(folder);
        }
    }

    [Fact]
    public async Task DecorativeModesPersistSeparatelyAndCustomIsSelectable()
    {
        string folder=Path.Combine(Path.GetTempPath(),"RSS_II_RGB-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"settings.json");
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var vm=new MainViewModel(controller,new SettingsService(path),new NoStartup());
            vm.RestoreSettings();
            Assert.Equal(0,vm.LightBarModeIndex);Assert.Equal(0,vm.EyeModeIndex);
            vm.LightBarModeIndex=1;
            Assert.Equal(DecorationMode.Spatial,controller.LightBarMode);
            Assert.Equal(DecorationMode.FollowKeys,controller.EyeMode);
            var restored=new MainViewModel(controller,new SettingsService(path),new NoStartup());
            restored.RestoreSettings();
            Assert.Equal(1,restored.LightBarModeIndex);Assert.Equal(0,restored.EyeModeIndex);
            restored.EyeModeIndex=1;restored.LightBarModeIndex=0;
            var saved=new SettingsService(path).Settings;
            Assert.Equal(DecorationMode.FollowKeys,saved.LightBarMode);Assert.Equal(DecorationMode.Spatial,saved.EyeMode);
            restored.EyeModeIndex=2;restored.LightBarModeIndex=999;
            Assert.Equal(2,restored.EyeModeIndex);Assert.Equal(0,restored.LightBarModeIndex);
            saved=new SettingsService(path).Settings;
            Assert.Equal(DecorationMode.FollowKeys,saved.LightBarMode);Assert.Equal(DecorationMode.Custom,saved.EyeMode);
        }
        finally{File.Delete(path);Directory.Delete(folder);}
    }

    private static Rgb[] Render(KeyboardController controller)
    {
        var compositor = new Compositor(controller.Profile.LedCount);
        foreach (var layer in controller.BuildLayers()) compositor.Add(layer);
        var frame = new LedFrame(controller.Profile.LedCount);
        compositor.Compose(frame, new EffectContext(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16), [], controller.Profile));
        return frame.Pixels.ToArray();
    }
    private sealed class QuietLog : ILogSink
    {
        public void Log(LogLevel level, string message, Exception? exception = null) { }
    }
    private sealed class NoStartup : IStartupManager
    {
        public bool IsSupported => false;
        public bool IsEnabled() => false;
        public void SetEnabled(bool enabled) { }
    }
}
