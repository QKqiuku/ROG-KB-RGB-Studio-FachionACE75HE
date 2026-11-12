using RSS_II_RGB.App;
using Avalonia.Media;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;
namespace ROG_KB_RGB_Studio.App.Tests;
public class RegionParametersTests
{
    [Fact]
    public async Task RegionCanReturnToRainbowAndKeepsIndependentParametersAfterReload()
    {
        string path=Path.GetTempFileName();await using var c=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);var e=new ZoneEditorViewModel(c,store);
            e.Keys.Single(k=>k.Name=="A").IsSelected=true;e.ZoneEffect=EffectChoice.Wave;e.ZoneColor=Colors.Red;
            Assert.False(e.ZoneRainbow);e.ZoneSpeed=2.4;e.ZoneWidth=1.6;e.ZoneBrightness=42;e.ZoneMirrorX=true;
            e.AssignZoneCommand.Execute(null);e.ZoneRows[0].EditCommand.Execute(null);
            Assert.Equal(2.4,e.ZoneSpeed);Assert.False(e.ZoneRainbow);e.ZoneRainbow=true;e.AssignZoneCommand.Execute(null);
            var reloaded=new SettingsService(path);var p=Assert.Single(reloaded.Settings.Zones).Parameters!;
            Assert.True(p.Rainbow);Assert.Equal(2.4,p.Speed);Assert.Equal(1.6,p.Width);Assert.Equal(42,p.Brightness);Assert.True(p.MirrorX);
            var editor=new ZoneEditorViewModel(c,reloaded);editor.ZoneRows[0].EditCommand.Execute(null);
            Assert.True(editor.ZoneRainbow);Assert.Equal(Colors.Red,editor.ZoneColor);Assert.Equal(42,editor.ZoneBrightness);
            c.SetGlobalEffect(EffectChoice.Off,Rgb.Black,1);c.SetZones([ZoneMapping.ToZone(reloaded.Settings.Zones[0])]);
            var comp=new Compositor(c.Profile.LedCount);foreach(var l in c.BuildLayers())comp.Add(l);
            var frame=new LedFrame(c.Profile.LedCount);bool colourful=false;
            for(int step=0;step<20;step++)
            {
                comp.Compose(frame,new EffectContext(TimeSpan.FromSeconds(step*.13),TimeSpan.FromSeconds(.13),[],c.Profile));
                var rgb=frame[c.Profile.IndexForKeyName("A")];colourful|=rgb.G>0 || rgb.B>0;
                Assert.InRange(Math.Max(rgb.R,Math.Max(rgb.G,rgb.B)),0,108);
                Assert.All(frame.Pixels.ToArray().Where((_,i)=>i!=c.Profile.IndexForKeyName("A")),rgb=>Assert.Equal(Rgb.Black,rgb));
            }
            Assert.True(colourful);
        }
        finally{File.Delete(path);}
    }
    [Fact]
    public async Task NamedCompositionRestoresRegionParametersInsteadOfRevivingTheDraft()
    {
        string path=Path.GetTempFileName();await using var c=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);var vm=new MainViewModel(c,store,new Startup());vm.RestoreSettings();vm.IsCustomMode=true;
            vm.Editor!.CanSelectKeys=true;vm.Editor.Keys[0].IsSelected=true;vm.Editor.ZoneEffect=EffectChoice.Wave;
            vm.Editor.ZoneSpeed=2;vm.Editor.ZoneRainbow=true;vm.Editor.ZoneBrightness=53;vm.Editor.AssignZoneCommand.Execute(null);
            vm.UserPresetName="region";await vm.SaveUserPresetCommand.ExecuteAsync(null);
            vm.RegularPresets.Single(p=>p.Effect==EffectChoice.Solid).SelectCommand.Execute(null);vm.ChooseCustomCommand.Execute(null);
            Assert.Empty(vm.Editor.ZoneRows);vm.UserPresets[0].LoadCommand.Execute(null);
            vm.Editor!.ZoneRows[0].EditCommand.Execute(null);Assert.Equal(2,vm.Editor.ZoneSpeed);Assert.True(vm.Editor.ZoneRainbow);Assert.Equal(53,vm.Editor.ZoneBrightness);
        }
        finally{File.Delete(path);}
    }
    [Fact]
    public async Task MusicRegionRequestsCaptureAndReusesHistoryOnlyForTheSameMask()
    {
        string path=Path.GetTempFileName();await using var c=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var vm=new MainViewModel(c,new SettingsService(path),new Startup());vm.RestoreSettings();vm.IsCustomMode=true;vm.SelectedEffect=EffectChoice.Off;
            vm.Editor!.CanSelectKeys=true;vm.Editor.Keys[0].IsSelected=true;vm.Editor.ZoneEffect=EffectChoice.MusicClassicEqualizer;vm.Editor.AssignZoneCommand.Execute(null);
            Assert.True(c.NeedsAudio);Assert.True(vm.ShowMusicControls);
            var first=Assert.Single(c.BuildLayers().OfType<MusicLayer>());Assert.Same(first,Assert.Single(c.BuildLayers().OfType<MusicLayer>()));
            c.SetZones([new Zone([1],EffectChoice.MusicClassicEqualizer,c.Zones[0].Color,Parameters:c.Zones[0].Parameters)]);
            var changed=Assert.Single(c.BuildLayers().OfType<MusicLayer>());Assert.NotSame(first,changed);Assert.False(changed.Mask.Contains(0));Assert.True(changed.Mask.Contains(1));
            vm.Editor.ClearZonesCommand.Execute(null);Assert.False(c.NeedsAudio);Assert.False(vm.ShowMusicControls);
        }
        finally{File.Delete(path);}
    }
    [Fact]
    public async Task StripRegionUsesItsParametersWithoutDimmingTheKeyboardOrEye()
    {
        await using var c=new KeyboardController(new QuietLog(),new SensorState());
        typeof(KeyboardController).GetField("_profile",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(c,RSS_II_RGB.Core.Layout.Falchion75Layout.Profile);
        c.LightBarMode=RSS_II_RGB.Core.Engine.DecorationMode.Custom;c.EyeMode=RSS_II_RGB.Core.Engine.DecorationMode.Custom;
        c.BarEffect=new() {Effect=DecorationEffect.Segmented};c.EyeEffect=new() {Color=new Rgb(0,255,0)};
        c.SetGlobalEffect(EffectChoice.Solid,new Rgb(0,0,80),1);
        c.SetZones([new Zone([83,84],EffectChoice.Rainbow,new Rgb(255,0,0),Parameters:new EffectSetting {Rainbow=true,Brightness=25,Speed=2})]);
        var lighting=new RSS_II_RGB.Core.Engine.Falchion75Lighting();lighting.Configure(c.BuildLayers(true),RSS_II_RGB.Core.Engine.DecorationMode.Custom,RSS_II_RGB.Core.Engine.DecorationMode.Custom);
        var keys=new LedFrame(83);keys.Pixels.Fill(new Rgb(0,0,80));bool colourful=false;
        for(int step=0;step<12;step++)
        {
            var frame=lighting.Compose(keys,new EffectContext(TimeSpan.FromSeconds(step*.2),TimeSpan.FromSeconds(.2),[],c.Profile));
            foreach(int i in new[]{83,84}){var rgb=frame[i];colourful|=rgb.G>0 || rgb.B>0;Assert.InRange(Math.Max(rgb.R,Math.Max(rgb.G,rgb.B)),0,65);}
            Assert.All(frame.Pixels[..83].ToArray(),rgb=>Assert.Equal(new Rgb(0,0,80),rgb));Assert.Equal(new Rgb(0,255,0),frame[95]);
        }
        Assert.True(colourful);
    }
    [Fact]
    public async Task LegacyRegionEditorsDoNotAdvertiseUnusedSpeedOrSaturation()
    {
        string path=Path.GetTempFileName();await using var c=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var e=new ZoneEditorViewModel(c,new SettingsService(path));
            foreach(var effect in new[]{EffectChoice.Starlight,EffectChoice.Audio})
            {e.ZoneEffect=effect;e.ZoneRainbow=true;Assert.False(e.ZoneHasSpeed);Assert.False(e.ZoneShowSaturation);}
            e.ZoneEffect=EffectChoice.Wave;e.ZoneRainbow=true;Assert.True(e.ZoneHasSpeed);Assert.True(e.ZoneShowSaturation);
        }
        finally{File.Delete(path);}
    }
    private sealed class QuietLog:ILogSink {public void Log(LogLevel level,string message,Exception? exception=null){}}
    private sealed class Startup:IStartupManager {public bool IsEnabled()=>false;public void SetEnabled(bool enabled){}public bool IsSupported=>false;}
}
