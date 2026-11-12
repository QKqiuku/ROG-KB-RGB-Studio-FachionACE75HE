using RSS_II_RGB.App;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;
namespace ROG_KB_RGB_Studio.App.Tests;
public class PresetCleanupTests
{
    [Fact] public void RetiredChoicesAreAbsentFromEveryEditorAndLibrary()
    {
        foreach(var e in new[]{EffectChoice.Trail,EffectChoice.Stream,EffectChoice.Windmill,EffectChoice.SpiralRing,EffectChoice.Nailong,EffectChoice.MulticolorSineSectors})
        {Assert.DoesNotContain(e,PresetCatalog.All);Assert.True(PresetCatalog.IsRemoved(e));Assert.Null(PresetCatalog.PatternFor(e));}
        Assert.Single(PresetCatalog.Regular,e=>e==EffectChoice.SineSectors);Assert.False(PresetCatalog.DefaultSettings(EffectChoice.SineSectors).Rainbow);
    }
    [Fact] public async Task OldMultiColourSettingsAndNamedRegionsMigrateWithoutLosingTheirPalette()
    {
        string path=Path.GetTempFileName();await using var c=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);store.Settings.GlobalEffect=EffectChoice.MulticolorSineSectors;
            store.Settings.EffectSettings[EffectChoice.MulticolorSineSectors]=new(){Speed=2.1,Width=1.6,Rainbow=true};
            store.Settings.FavoriteEffects=[EffectChoice.MulticolorSineSectors,EffectChoice.SineSectors,EffectChoice.Trail];
            store.Settings.Zones=[new(){KeyIndices=[0],Effect=EffectChoice.MulticolorSineSectors,Parameters=new(){Speed=1.8,Rainbow=true}}];
            var vm=new MainViewModel(c,store,new Startup());vm.RestoreSettings();
            Assert.Equal(EffectChoice.SineSectors,vm.SelectedEffect);Assert.True(vm.RainbowColors);Assert.Equal(2.1,vm.EffectSpeed);Assert.Equal(1.6,vm.EffectWidth);
            Assert.Equal(new[]{EffectChoice.SineSectors},store.Settings.FavoriteEffects);
            var zone=Assert.Single(c.Zones);Assert.Equal(EffectChoice.SineSectors,zone.Effect);Assert.True(zone.Parameters!.Rainbow);Assert.Equal(1.8,zone.Parameters.Speed);
            var named=new LightingComposition{BaseEffect=EffectChoice.MulticolorSineSectors,Base=new(){Speed=2.7,Rainbow=true},Zones=[new(){KeyIndices=[0],Effect=EffectChoice.MulticolorSineSectors}]};
            named.ApplyTo(store.Settings,c.Profile.LedCount);vm.RestoreSettings();
            Assert.Equal(EffectChoice.SineSectors,vm.SelectedEffect);Assert.True(vm.RainbowColors);Assert.Equal(2.7,vm.EffectSpeed);
            Assert.True(Assert.Single(c.Zones).Parameters!.Rainbow);
            Assert.Equal(EffectChoice.MulticolorSineSectors,named.BaseEffect);Assert.True(named.Base.Rainbow);
            var mono=ZoneMapping.ToZone(new(){Effect=EffectChoice.MulticolorSineSectors,Parameters=new(){Rainbow=false},ColorHex="FF0000"});
            Assert.False(mono.Parameters!.Rainbow);Assert.Equal(EffectChoice.SineSectors,mono.Effect);
            named.Base.Rainbow=false;named.ApplyTo(store.Settings,c.Profile.LedCount);vm.RestoreSettings();Assert.False(vm.RainbowColors);
        }
        finally{File.Delete(path);}
    }
    private sealed class QuietLog:ILogSink {public void Log(LogLevel l,string m,Exception? e=null){}}
    private sealed class Startup:IStartupManager {public bool IsEnabled()=>false;public void SetEnabled(bool enabled){}public bool IsSupported=>false;}
}
