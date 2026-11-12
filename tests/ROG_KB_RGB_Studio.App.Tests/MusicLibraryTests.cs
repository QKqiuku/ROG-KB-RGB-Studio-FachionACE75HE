using RSS_II_RGB.App;
namespace ROG_KB_RGB_Studio.App.Tests;
public class MusicLibraryTests
{
    [Fact]
    public void MusicLibraryKeepsOnlyTheElevenRequestedChoices()
    {
        Assert.Equal(11,PresetCatalog.Music.Length);
        Assert.DoesNotContain(EffectChoice.MusicFlowNeonSpectrum,PresetCatalog.All);
        Assert.DoesNotContain(EffectChoice.MusicSoftSpectrum,PresetCatalog.All);
        Assert.Equal(EffectChoice.MusicSpectrumBars,PresetCatalog.Available(EffectChoice.MusicBottomEnergyCore));
    }

    [Fact]
    public void PrerecordedReferenceAssetsAreNotBundled()
    {
        var assets=new Avalonia.Platform.StandardAssetLoader().GetAssets(new Uri("avares://RSS_II_RGB.App/Assets"),null);
        Assert.DoesNotContain(assets,asset=>asset.AbsolutePath.EndsWith("LumoMusicReference.bin",StringComparison.Ordinal));
    }
    [Fact]
    public async Task RetiredMusicMigratesWithoutKeepingDeadFavouritesOrChangingAudioSource()
    {
        var retired=Enum.GetValues<EffectChoice>().Where(e=>e>=EffectChoice.MusicSpectrumBars && PresetCatalog.IsRemoved(e)).ToArray();
        Assert.Equal(27,retired.Length);
        foreach(var effect in retired)
        {
            string path=Path.GetTempFileName();
            await using var controller=new KeyboardController(new QuietLog(),new RSS_II_RGB.Core.Sensors.SensorState());
            try
            {
                var store=new SettingsService(path);store.Settings.GlobalEffect=effect;store.Settings.CustomBaseEffect=effect;
                store.Settings.FavoriteEffects=[effect];store.Settings.RecentEffects=[effect];
                store.Settings.MusicSourcePath="C:/Player.exe";store.Settings.MusicSourceName="Player";
                store.Settings.EffectSettings[effect]=new EffectSetting();
                var vm=new MainViewModel(controller,store,new Startup());vm.RestoreSettings();
                Assert.Equal(EffectChoice.MusicSpectrumBars,vm.SelectedEffect);
                Assert.Equal(EffectChoice.MusicSpectrumBars,store.Settings.CustomBaseEffect);
                Assert.DoesNotContain(effect,store.Settings.FavoriteEffects);Assert.DoesNotContain(effect,store.Settings.RecentEffects);
                Assert.False(store.Settings.EffectSettings.ContainsKey(effect));Assert.Equal("C:/Player.exe",store.Settings.MusicSourcePath);
                Assert.Null(PresetCatalog.MusicFor(effect));
            }
            finally {File.Delete(path);}
        }
    }
    private sealed class QuietLog:RSS_II_RGB.Core.Logging.ILogSink {public void Log(RSS_II_RGB.Core.Logging.LogLevel level,string message,Exception? exception=null){}}
    private sealed class Startup:RSS_II_RGB.Core.Startup.IStartupManager {public bool IsEnabled()=>false;public void SetEnabled(bool enabled){}public bool IsSupported=>false;}
}
