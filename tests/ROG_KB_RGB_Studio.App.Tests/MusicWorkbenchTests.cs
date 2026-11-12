using RSS_II_RGB.App;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;
namespace ROG_KB_RGB_Studio.App.Tests;
public class MusicWorkbenchTests
{
    [Fact]
    public async Task OverallAndDecorativeEffectsSaveIndependentlyAndDriveAudioCapture()
    {
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var audio=new FakeAudio();var vm=new MainViewModel(controller,new SettingsService(path),new Startup(),audio);vm.RestoreSettings();
            vm.SelectedEffect=EffectChoice.Solid;vm.IsCustomMode=true;
            vm.OverallModeIndex=2;vm.OverallOpacity=70;vm.OverallMinimum=15;vm.OverallSpeed=1.5;
            Assert.True(vm.ShowMusicControls);Assert.True(controller.NeedsAudio);
            await vm.ConfigureMusicAudioAsync(TestContext.Current.CancellationToken);Assert.True(audio.Last!.CaptureEnabled);
            var envelope=controller.BuildOutputEnvelope();vm.TargetFps=30;Assert.Same(envelope,controller.BuildOutputEnvelope());
            vm.OverallModeIndex=0;vm.Editor!.SelectDecoration(false);
            Assert.Equal(2,vm.LightBarModeIndex);vm.BarEditor.EffectIndex=1;vm.BarEditor.Color=Avalonia.Media.Colors.Cyan;
            Assert.True(controller.NeedsAudio);
            vm.Editor.SelectDecoration(true);vm.EyeEditor.EffectIndex=3;vm.EyeEditor.Color=Avalonia.Media.Colors.Magenta;vm.EyeEditor.Speed=2;
            var loaded=new MainViewModel(controller,new SettingsService(path),new Startup(),audio);loaded.RestoreSettings();
            Assert.Equal(70,loaded.OverallOpacity);Assert.Equal(15,loaded.OverallMinimum);Assert.Equal(1.5,loaded.OverallSpeed);
            Assert.Equal(2,loaded.LightBarModeIndex);Assert.Equal(2,loaded.EyeModeIndex);
            Assert.Equal(1,loaded.BarEditor.EffectIndex);Assert.Equal(Avalonia.Media.Colors.Cyan,loaded.BarEditor.Color);
            Assert.Equal(3,loaded.EyeEditor.EffectIndex);Assert.Equal(Avalonia.Media.Colors.Magenta,loaded.EyeEditor.Color);Assert.Equal(2,loaded.EyeEditor.Speed);
            loaded.IsCustomMode=false;Assert.False(controller.NeedsAudio);Assert.Null(controller.BuildOutputEnvelope());
            await loaded.ConfigureMusicAudioAsync(TestContext.Current.CancellationToken);Assert.False(audio.Last!.CaptureEnabled);
        }
        finally {File.Delete(path);}
    }
    [Fact]
    public async Task IndependentWaveColoursSurvivePresetSwitchAndReload()
    {
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var vm=new MainViewModel(controller,new SettingsService(path),new Startup());vm.RestoreSettings();
            vm.SelectedEffect=EffectChoice.MusicSoundWave;
            Assert.True(vm.IsSoundWave);Assert.False(vm.ShowSingleColor);
            vm.WaveColor1=Avalonia.Media.Color.FromRgb(255,0,0);vm.WaveColor2=Avalonia.Media.Color.FromRgb(0,0,255);
            Assert.False(vm.RainbowColors);
            vm.SelectedEffect=EffectChoice.MusicSpectrumBars;vm.SelectedEffect=EffectChoice.MusicSoundWave;
            Assert.Equal((byte)255,vm.WaveColor1.R);Assert.Equal((byte)255,vm.WaveColor2.B);
            vm.RainbowColors=true;
            var loaded=new MainViewModel(controller,new SettingsService(path),new Startup());loaded.RestoreSettings();
            Assert.True(loaded.RainbowColors);
            Assert.Equal(vm.WaveColor1,loaded.WaveColor1);Assert.Equal(vm.WaveColor2,loaded.WaveColor2);
            Assert.Equal(new RSS_II_RGB.Core.Rendering.Rgb(255,0,0),controller.BaseOptions.WaveColor1);
            Assert.Equal(new RSS_II_RGB.Core.Rendering.Rgb(0,0,255),controller.BaseOptions.WaveColor2);
        }
        finally {File.Delete(path);}
    }

    [Fact]
    public void VortexIsRemovedFromAllSelectablePresetsAndMigratesSafely()
    {
        Assert.True(PresetCatalog.IsRemoved(EffectChoice.Vortex));
        Assert.DoesNotContain(EffectChoice.Vortex,PresetCatalog.All);
        Assert.Equal(EffectChoice.Rainbow,PresetCatalog.Available(EffectChoice.Vortex));
    }
    [Fact]
    public void MusicCataloguePreservesEnumValuesAndExposesElevenChoices()
    {
        var choices=Enum.GetValues<EffectChoice>().Where(e=>e.ToString().StartsWith("Music",StringComparison.Ordinal)).ToArray();
        Assert.Equal(38,choices.Length);
        Assert.Equal(11,PresetCatalog.Music.Length);
        Assert.All(choices.Where(PresetCatalog.IsRemoved),e=>Assert.DoesNotContain(e,PresetCatalog.All));
    }

    [Fact]
    public async Task ProcessChoiceControlsCaptureAndKeepsMusicHistoryAcrossUnrelatedEdits()
    {
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var control=new FakeAudio {Sources=[new(123,"Player",100,"C:/Player.exe")]};
            var vm=new MainViewModel(controller,new SettingsService(path),new Startup(),control);vm.RestoreSettings();
            await vm.RefreshAudioSourcesAsync();
            vm.SelectedMusicSource=vm.MusicSources.Single(x=>x.Source.ProcessId==123);
            vm.MusicPresets.Single(x=>x.Effect==EffectChoice.MusicSpectrumBars).SelectCommand.Execute(null);
            await vm.ConfigureMusicAudioAsync(TestContext.Current.CancellationToken);
            Assert.True(control.Last!.CaptureEnabled);Assert.Equal(123,control.Last.ProcessId);Assert.Equal(100,control.Last.StartTimeTicks);
            control.Sources=[new(456,"Player",200,"C:/Player.exe")];
            await vm.RefreshAudioSourcesAsync();await vm.ConfigureMusicAudioAsync(TestContext.Current.CancellationToken);
            Assert.Equal(456,control.Last!.ProcessId);Assert.True(control.Last.CaptureEnabled);
            var layer=Assert.Single(controller.BuildLayers().OfType<MusicLayer>());
            vm.TargetFps=30;
            Assert.Same(layer,Assert.Single(controller.BuildLayers().OfType<MusicLayer>()));
            Assert.NotSame(layer,Assert.Single(controller.BuildLayers(spatial:true).OfType<MusicLayer>()));
            vm.RegularPresets.Single(x=>x.Effect==EffectChoice.Wave).SelectCommand.Execute(null);
            await vm.ConfigureMusicAudioAsync(TestContext.Current.CancellationToken);Assert.False(control.Last!.CaptureEnabled);
            Assert.Equal("C:/Player.exe",new SettingsService(path).Settings.MusicSourcePath);
        }
        finally {File.Delete(path);}
    }
    [Fact]
    public async Task AmbiguousSavedProcessNeverFallsBackToSystemCapture()
    {
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);
            store.Settings.GlobalEffect=EffectChoice.MusicSpectrumBars;
            store.Settings.MusicSourcePath="C:/Player.exe";store.Settings.MusicSourceName="Player";
            var control=new FakeAudio {Sources=[new(123,"Player",100,"C:/Player.exe"),new(124,"Player",101,"C:/Player.exe")]};
            var vm=new MainViewModel(controller,store,new Startup(),control);vm.RestoreSettings();
            await vm.RefreshAudioSourcesAsync();await vm.ConfigureMusicAudioAsync(TestContext.Current.CancellationToken);
            Assert.False(control.Last!.CaptureEnabled);Assert.Equal(-1,vm.SelectedMusicSource!.Source.ProcessId);
            control.Sources=[new(123,"Player",100,"C:/Player.exe")];
            await vm.RefreshAudioSourcesAsync();await vm.ConfigureMusicAudioAsync(TestContext.Current.CancellationToken);
            Assert.True(control.Last!.CaptureEnabled);Assert.Equal(123,control.Last.ProcessId);
        }
        finally {File.Delete(path);}
    }
    [Fact]
    public async Task MusicOptionsAndFavouriteSurviveReloadWithoutChangingFrameRateOrKeyLayer()
    {
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var vm=new MainViewModel(controller,new SettingsService(path),new Startup());vm.RestoreSettings();
            vm.MusicPresets[0].SelectCommand.Execute(null);vm.ToggleFavoriteCommand.Execute(null);
            vm.MusicSensitivity=2;vm.MusicRelease=.42;vm.MusicAutoGain=false;vm.TargetFps=45;
            vm.ReactivePresets.Single(x=>x.Effect==EffectChoice.Ripple).SelectCommand.Execute(null);
            var loaded=new MainViewModel(controller,new SettingsService(path),new Startup());loaded.RestoreSettings();
            Assert.Equal(11,loaded.MusicPresets.Length);Assert.True(loaded.IsMusicEffect);Assert.True(loaded.IsCustomMode);
            Assert.True(loaded.EnableReactive);Assert.Equal(EffectChoice.Ripple,loaded.SelectedReactiveEffect);
            Assert.Equal(2,loaded.MusicSensitivity);Assert.Equal(.42,loaded.MusicRelease);Assert.False(loaded.MusicAutoGain);
            Assert.Equal(45,loaded.TargetFps);Assert.Contains(loaded.FavoritePresets,x=>x.Effect==EffectChoice.MusicSpectrumBars);
        }
        finally {File.Delete(path);}
    }
    private sealed class FakeAudio:IAudioControl
    {
        public AudioSource[] Sources {get;set;}=[];
        public AudioControlRequest? Last {get;private set;}
        public Task<AudioControlResponse> SendAsync(AudioControlRequest request,CancellationToken ct=default)
        {
            if(request.Command=="configure") Last=request;
            return Task.FromResult(new AudioControlResponse {Sources=Sources,State=request.CaptureEnabled?"capturing":"stopped",ProcessCaptureSupported=true});
        }
    }
    private sealed class QuietLog:ILogSink {public void Log(LogLevel level,string message,Exception? exception=null){}}
    private sealed class Startup:IStartupManager {public bool IsEnabled()=>false;public void SetEnabled(bool enabled){}public bool IsSupported=>false;}
}
