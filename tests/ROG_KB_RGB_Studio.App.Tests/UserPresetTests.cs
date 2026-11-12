using RSS_II_RGB.App;
using Avalonia.Media;
using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;
namespace ROG_KB_RGB_Studio.App.Tests;
public class UserPresetTests
{
    [Fact]
    public async Task EveryBuiltInResetsItsOwnParametersWithoutChangingTheComposition()
    {
        string path=Path.GetTempFileName();await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var vm=new MainViewModel(controller,new SettingsService(path),new Startup());vm.RestoreSettings();vm.IsCustomMode=true;
            vm.TargetFps=30;vm.OverallModeIndex=1;vm.OverallOpacity=65;vm.BarEditor.EffectIndex=3;
            vm.EnableReactive=true;vm.SelectedReactiveEffect=EffectChoice.KeyLightning;vm.ReactiveSpeed=2;
            controller.SetZones([new Zone([0],EffectChoice.Solid,new Rgb(255,0,0))]);
            foreach(var effect in PresetCatalog.All)
            {
                vm.SelectedEffect=effect;vm.EffectSpeed=4;vm.EffectWidth=2;vm.EffectDensity=85;
                vm.EffectSaturation=30;vm.EffectDirection=90;vm.MirrorX=true;vm.MirrorY=true;vm.BrightnessPercent=22;
                vm.ResetCurrentEffectCommand.Execute(null);
                Assert.Equal(effect,vm.SelectedEffect);Assert.Equal(1,vm.EffectSpeed);Assert.Equal(1,vm.EffectWidth);
                Assert.Equal(30,vm.EffectDensity);Assert.Equal(100,vm.EffectSaturation);Assert.Equal(0,vm.EffectDirection);
                Assert.False(vm.MirrorX);Assert.False(vm.MirrorY);Assert.Equal(100,vm.BrightnessPercent);
                Assert.Equal(PresetCatalog.DefaultRainbow(effect),vm.RainbowColors);
                Assert.Equal(30,vm.TargetFps);Assert.Equal(65,vm.OverallOpacity);Assert.Equal(3,vm.BarEditor.EffectIndex);
                Assert.Equal(2,vm.ReactiveSpeed);Assert.Single(controller.Zones);
            }
            vm.ReactiveSpeed=3;vm.ReactiveWidth=2;vm.ReactiveBrightness=40;vm.ReactiveColor=Colors.Red;
            vm.ResetReactiveEffectCommand.Execute(null);
            Assert.Equal(1,vm.ReactiveSpeed);Assert.Equal(1,vm.ReactiveWidth);Assert.Equal(100,vm.ReactiveBrightness);
            Assert.True(vm.ReactivePalette);Assert.Equal(Color.FromRgb(0,180,255),vm.ReactiveColor);Assert.Equal(65,vm.OverallOpacity);
        }
        finally {File.Delete(path);}
    }
    [Fact]
    public async Task NamedCompositionsAreIndependentEditableAndKeepAudioTargetAndFrameRate()
    {
        string path=Path.GetTempFileName();await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);var vm=new MainViewModel(controller,store,new Startup());vm.RestoreSettings();
            vm.IsCustomMode=true;vm.SelectedEffect=EffectChoice.MusicSoundWave;vm.EffectSpeed=1.7;
            vm.WaveColor1=Colors.Red;vm.WaveColor2=Colors.Blue;vm.EnableReactive=true;vm.SelectedReactiveEffect=EffectChoice.KeyLightning;vm.ReactiveWidth=1.6;
            vm.OverallModeIndex=1;vm.OverallOpacity=73;vm.LightBarModeIndex=2;vm.BarEditor.EffectIndex=4;vm.EyeModeIndex=2;vm.EyeEditor.EffectIndex=3;
            vm.MusicSensitivity=2;store.Settings.Zones=[new ZoneSetting {KeyIndices=[83,94],ColorHex="FF0000"}];
            vm.UserPresetName="夜间节奏";await vm.SaveUserPresetCommand.ExecuteAsync(null);
            string first=Assert.Single(vm.UserPresets).Id;
            vm.EffectSpeed=3;vm.OverallOpacity=20;store.Settings.Zones[0].KeyIndices[0]=84;
            vm.UserPresetName="明亮节奏";await vm.SaveUserPresetAsCommand.ExecuteAsync(null);Assert.Equal(2,vm.UserPresets.Count);
            vm.EffectSpeed=4;vm.TargetFps=45;
            vm.SelectedMusicSource=new MusicSourceItem(new AudioSource(321,"Player",42,"C:/Player.exe"));
            vm.UserPresets.Single(p=>p.Id==first).LoadCommand.Execute(null);
            Assert.True(vm.IsCustomMode);Assert.Equal(1.7,vm.EffectSpeed);Assert.Equal(73,vm.OverallOpacity);Assert.Equal(45,vm.TargetFps);
            Assert.Equal(321,vm.SelectedMusicSource!.Source.ProcessId);Assert.Equal(42,vm.SelectedMusicSource.Source.StartTimeTicks);
            Assert.Equal(EffectChoice.MusicSoundWave,vm.SelectedEffect);Assert.Equal(Colors.Red,vm.WaveColor1);Assert.Equal(Colors.Blue,vm.WaveColor2);
            Assert.Equal(4,vm.BarEditor.EffectIndex);Assert.Equal(3,vm.EyeEditor.EffectIndex);Assert.Equal(1.6,vm.ReactiveWidth);
            Assert.Equal(new[]{83,94},store.Settings.Zones[0].KeyIndices);Assert.Equal(2,vm.MusicSensitivity);
            vm.EffectSpeed=2.4;await vm.SaveUserPresetCommand.ExecuteAsync(null);Assert.Equal(2,vm.UserPresets.Count);
            vm.EffectSpeed=4;vm.RestoreUserPresetCommand.Execute(null);Assert.Equal(2.4,vm.EffectSpeed);
            var loaded=new MainViewModel(controller,new SettingsService(path),new Startup());loaded.RestoreSettings();
            Assert.Equal(2,loaded.UserPresets.Count);Assert.True(loaded.CanRestoreUserPreset);Assert.Equal("夜间节奏",loaded.UserPresetName);
            loaded.UserPresets.Single(p=>p.Name=="明亮节奏").LoadCommand.Execute(null);Assert.Equal(3,loaded.EffectSpeed);Assert.Equal(20,loaded.OverallOpacity);
            loaded.RegularPresets.Single(p=>p.Effect==EffectChoice.Rainbow).SelectCommand.Execute(null);Assert.False(loaded.CanRestoreUserPreset);
        }
        finally {File.Delete(path);}
    }
    [Fact]
    public async Task NamesAreValidatedAndForeignKeyboardRecipesStayHidden()
    {
        string path=Path.GetTempFileName();await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);var vm=new MainViewModel(controller,store,new Startup());vm.RestoreSettings();vm.IsCustomMode=true;
            vm.UserPresetName="  ";await vm.SaveUserPresetCommand.ExecuteAsync(null);Assert.Empty(vm.UserPresets);
            vm.UserPresetName="test";await vm.SaveUserPresetCommand.ExecuteAsync(null);Assert.Single(vm.UserPresets);
            vm.UserPresetName=" TEST ";await vm.SaveUserPresetAsCommand.ExecuteAsync(null);Assert.Single(vm.UserPresets);Assert.Equal(L.UserPresetDuplicateName,vm.UserPresetStatus);
            var foreign=store.Settings.UserPresets[0] with {Id="foreign",Name="Other keyboard",KeyboardProfile="Other profile"};store.Settings.UserPresets.Add(foreign);store.Save();
            var loaded=new MainViewModel(controller,new SettingsService(path),new Startup());loaded.RestoreSettings();Assert.Single(loaded.UserPresets);
            Assert.Equal(2,new SettingsService(path).Settings.UserPresets.Count);
        }
        finally {File.Delete(path);}
    }
    [Fact]
    public async Task FailedSaveKeepsTheOldLibraryAndLeavesNoTemporaryFile()
    {
        string folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"settings.json");
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var vm=new MainViewModel(controller,new SettingsService(path),new Startup());vm.RestoreSettings();vm.IsCustomMode=true;vm.UserPresetName="saved";
            await vm.SaveUserPresetCommand.ExecuteAsync(null);string original=File.ReadAllText(path);
            using(var held=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {vm.UserPresetName="new";await vm.SaveUserPresetAsCommand.ExecuteAsync(null);}
            Assert.Single(vm.UserPresets);Assert.Equal(L.UserPresetSaveFailed,vm.UserPresetStatus);Assert.Equal(original,File.ReadAllText(path));
            Assert.Empty(Directory.GetFiles(folder,"*.tmp"));
        }
        finally {File.Delete(path);Directory.Delete(folder);}
    }
    [Fact]
    public async Task SavedGifKeepsItsFramesAfterTheImportCacheIsOverwritten()
    {
        string folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"settings.json"),gif=Path.Combine(folder,"current.kbanim");
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var profile=controller.Profile;
            void Write(Rgb colour)=>new RSS_II_RGB.Core.Animation.KbAnim(profile.Cols,profile.Rows,[new RSS_II_RGB.Core.Animation.KbAnim.Frame(100,Enumerable.Repeat(colour,profile.Cols*profile.Rows).ToArray())]).Save(gif);
            Write(new Rgb(255,0,0));var store=new SettingsService(path);store.Settings.GifAnimPath=gif;store.Settings.GlobalEffect=EffectChoice.Gif;
            var vm=new MainViewModel(controller,store,new Startup());vm.RestoreSettings();vm.IsCustomMode=true;vm.UserPresetName="red animation";
            await vm.SaveUserPresetCommand.ExecuteAsync(null);Assert.Single(vm.UserPresets);
            string immutable=store.Settings.UserPresets[0].Composition.GifAnimPath!;Assert.NotEqual(gif,immutable);
            Write(new Rgb(0,0,255));vm.UserPresets[0].LoadCommand.Execute(null);
            var comp=new RSS_II_RGB.Core.Effects.Compositor(profile.LedCount);foreach(var l in controller.BuildLayers())comp.Add(l);
            var frame=new RSS_II_RGB.Core.Rendering.LedFrame(profile.LedCount);
            comp.Compose(frame,new RSS_II_RGB.Core.Effects.EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],profile));
            Assert.All(frame.Pixels.ToArray(),p=>Assert.Equal(new Rgb(255,0,0),p));
            vm.EffectSpeed=2;await vm.SaveUserPresetCommand.ExecuteAsync(null);
            Assert.Single(Directory.GetFiles(store.UserPresetAssetDirectory,"*.kbanim"));
        }
        finally
        {
            foreach(string file in Directory.GetFiles(folder))File.Delete(file);
            string assets=Path.Combine(folder,"user-presets");if(Directory.Exists(assets)){foreach(string file in Directory.GetFiles(assets))File.Delete(file);Directory.Delete(assets);}
            Directory.Delete(folder);
        }
    }
    [Fact]
    public async Task DeleteRemovesOnlyTheSavedEntryAndKeepsCurrentLighting()
    {
        string path=Path.GetTempFileName();await using var c=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);var vm=new MainViewModel(c,store,new Startup());vm.RestoreSettings();vm.IsCustomMode=true;
            vm.SelectedEffect=EffectChoice.Wave;vm.EffectSpeed=1.8;vm.UserPresetName="first";await vm.SaveUserPresetCommand.ExecuteAsync(null);
            string first=Assert.Single(vm.UserPresets).Id;
            vm.UserPresetName="second";await vm.SaveUserPresetAsCommand.ExecuteAsync(null);
            await vm.UserPresets.Single(p=>p.Id==first).DeleteCommand.ExecuteAsync(null);
            Assert.Equal("second",Assert.Single(vm.UserPresets).Name);Assert.True(vm.CanRestoreUserPreset);
            Assert.Single(new SettingsService(path).Settings.UserPresets);
            await vm.UserPresets[0].DeleteCommand.ExecuteAsync(null);
            Assert.Empty(vm.UserPresets);Assert.Empty(new SettingsService(path).Settings.UserPresets);Assert.False(vm.CanRestoreUserPreset);
            Assert.True(vm.IsCustomMode);Assert.Equal(EffectChoice.Wave,vm.SelectedEffect);Assert.Equal(1.8,vm.EffectSpeed);Assert.Empty(vm.UserPresetName);
            vm.UserPresetName="new save";await vm.SaveUserPresetCommand.ExecuteAsync(null);Assert.Equal("new save",Assert.Single(vm.UserPresets).Name);
        }
        finally {File.Delete(path);}
    }
    [Fact]
    public async Task FailedDeleteKeepsSavedEntrySelectionAndOriginalFile()
    {
        string path=Path.GetTempFileName();await using var c=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var vm=new MainViewModel(c,new SettingsService(path),new Startup());vm.RestoreSettings();vm.IsCustomMode=true;vm.UserPresetName="keep";
            await vm.SaveUserPresetCommand.ExecuteAsync(null);var entry=Assert.Single(vm.UserPresets);string original=File.ReadAllText(path);
            using(var held=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))await entry.DeleteCommand.ExecuteAsync(null);
            Assert.Equal(entry.Id,Assert.Single(vm.UserPresets).Id);Assert.True(vm.UserPresets[0].IsSelected);Assert.True(vm.CanRestoreUserPreset);
            Assert.Equal("keep",vm.UserPresetName);Assert.Equal(original,File.ReadAllText(path));Assert.False(vm.IsSavingUserPreset);
            await entry.DeleteCommand.ExecuteAsync(null);Assert.Empty(vm.UserPresets);
        }
        finally {File.Delete(path);}
    }
    private sealed class QuietLog:ILogSink {public void Log(LogLevel level,string message,Exception? exception=null){}}
    private sealed class Startup:IStartupManager {public bool IsEnabled()=>false;public void SetEnabled(bool enabled){}public bool IsSupported=>false;}
}
