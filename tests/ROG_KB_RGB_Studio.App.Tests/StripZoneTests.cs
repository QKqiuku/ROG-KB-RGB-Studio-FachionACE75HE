using RSS_II_RGB.App;
using System.Reflection;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;
namespace ROG_KB_RGB_Studio.App.Tests;
public class StripZoneTests
{
    [Fact]
    public void EyeHasNoMusicOrSegmentedChoiceAndMigratesOldMusicToSolid()
    {
        var eye=new DecorationEditorViewModel(()=>{},true);
        Assert.Equal(new[]{0,2,3},eye.Choices.Select(c=>c.Value));
        eye.Load(new DecorationSetting {EffectIndex=1,ColorHex="0088FF"});
        Assert.Equal(0,eye.EffectIndex);Assert.False(eye.IsMusic);
        eye.EffectIndex=4;Assert.Equal(0,eye.EffectIndex);
        var bar=new DecorationEditorViewModel(()=>{});bar.EffectIndex=1;bar.MusicRainbow=true;
        var saved=bar.Save();var restored=new DecorationEditorViewModel(()=>{});restored.Load(saved);
        Assert.True(restored.MusicRainbow);Assert.Equal(1,restored.EffectIndex);
        Assert.Contains(bar.Choices,c=>c.Value==4);
    }
    [Fact]
    public async Task StripSelectionsPersistAndCannotAlterThe83KeysOrEye()
    {
        var state=new SensorState();await using var controller=new KeyboardController(new QuietLog(),state);
        // Test-only profile injection avoids touching the device-discovery service or global flags.
        typeof(KeyboardController).GetField("_profile",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(controller,Falchion75Layout.Profile);
        string path=Path.GetTempFileName();
        try
        {
            var vm=new MainViewModel(controller,new SettingsService(path),new Startup());vm.RestoreSettings();
            vm.IsCustomMode=true;vm.SelectedEffect=EffectChoice.Solid;vm.PickedColor=Avalonia.Media.Colors.Green;
            vm.LightBarModeIndex=2;vm.BarEditor.EffectIndex=4;vm.EyeModeIndex=2;vm.EyeEditor.Color=Avalonia.Media.Colors.Blue;
            var editor=vm.Editor!;editor.CanSelectKeys=true;
            Assert.True(editor.CanSelectBarSegments);Assert.Equal(12,editor.StripZoneSegments.Count);
            Assert.Equal(Enumerable.Range(83,12),editor.StripZoneSegments.Select(k=>k.Index));
            editor.StripZoneSegments[0].IsSelected=true;editor.StripZoneSegments[11].IsSelected=true;
            Assert.Equal(2,editor.SelectedCount);editor.ZoneEffect=EffectChoice.Solid;editor.ZoneColor=Avalonia.Media.Colors.Red;
            editor.AssignZoneCommand.Execute(null);
            Assert.Equal(new[]{83,94},Assert.Single(controller.Zones).KeyIndices);
            var keys=new Compositor(83);foreach(var l in controller.BuildLayers())keys.Add(l);
            var frame=new LedFrame(83);keys.Compose(frame,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile));
            Assert.All(frame.Pixels.ToArray(),p=>Assert.Equal(new Rgb(0,128,0),p));
            var lighting=new Falchion75Lighting();lighting.Configure(controller.BuildLayers(true),DecorationMode.Custom,DecorationMode.Custom);
            var final=lighting.Compose(frame,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile));
            Assert.Equal(new Rgb(255,0,0),final[83]);Assert.Equal(new Rgb(255,0,0),final[94]);
            Assert.All(final.Pixels.Slice(84,10).ToArray(),p=>Assert.Equal(Rgb.Black,p));Assert.Equal(new Rgb(0,0,255),final[95]);
            var loaded=new MainViewModel(controller,new SettingsService(path),new Startup());loaded.RestoreSettings();
            Assert.Equal(4,loaded.BarEditor.EffectIndex);Assert.Equal(new[]{83,94},Assert.Single(controller.Zones).KeyIndices);
            loaded.Editor!.ZoneRows[0].EditCommand.Execute(null);
            Assert.Equal(2,loaded.Editor.SelectedCount);Assert.True(loaded.Editor.StripZoneSegments[0].IsSelected);Assert.True(loaded.Editor.StripZoneSegments[11].IsSelected);
            loaded.BarEditor.EffectIndex=0;Assert.Equal(0,loaded.Editor.SelectedCount);
        }
        finally {File.Delete(path);}
    }
    private sealed class QuietLog:ILogSink {public void Log(LogLevel level,string message,Exception? exception=null){}}
    private sealed class Startup:IStartupManager {public bool IsEnabled()=>false;public void SetEnabled(bool enabled){}public bool IsSupported=>false;}
}
