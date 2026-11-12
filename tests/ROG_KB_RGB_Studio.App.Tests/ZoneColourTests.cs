using RSS_II_RGB.App;
using System.Reflection;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
namespace ROG_KB_RGB_Studio.App.Tests;
public class ZoneColourTests
{
    [Theory]
    [InlineData((int)EffectChoice.Wave)]
    [InlineData((int)EffectChoice.Rainbow)]
    [InlineData((int)EffectChoice.Aurora)]
    [InlineData((int)EffectChoice.Ocean)]
    [InlineData((int)EffectChoice.SineWave)]
    [InlineData((int)EffectChoice.SineSectors)]
    public async Task RegionUsesItsChosenColourInsteadOfThePresetsRainbow(int value)
    {
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        int[] indices=[controller.Profile.IndexForKeyName("A"),controller.Profile.IndexForKeyName("S"),controller.Profile.IndexForKeyName("D")];
        controller.SetGlobalEffect(EffectChoice.Solid,new Rgb(0,0,80),1);
        controller.SetZones([new Zone(indices,(EffectChoice)value,new Rgb(255,0,0))]);
        var comp=new Compositor(controller.Profile.LedCount);foreach(var layer in controller.BuildLayers())comp.Add(layer);
        var frame=new LedFrame(controller.Profile.LedCount);bool lit=false;
        for(int step=0;step<20;step++)
        {
            comp.Compose(frame,new EffectContext(TimeSpan.FromSeconds(step*.1),TimeSpan.FromSeconds(.1),[],controller.Profile));
            foreach(int i in indices){Assert.Equal(0,frame[i].G);Assert.Equal(0,frame[i].B);lit|=frame[i].R>0;}
            for(int i=0;i<frame.Length;i++)if(!indices.Contains(i))Assert.Equal(new Rgb(0,0,80),frame[i]);
        }
        Assert.True(lit);
    }
    [Fact]
    public async Task StripPatternUsesSelectedColourWithoutChangingKeysOrEye()
    {
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        typeof(KeyboardController).GetField("_profile",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(controller,Falchion75Layout.Profile);
        controller.LightBarMode=DecorationMode.Custom;controller.EyeMode=DecorationMode.Custom;
        controller.BarEffect=new() {Effect=DecorationEffect.Segmented};controller.EyeEffect=new() {Color=new Rgb(0,255,0)};
        controller.SetGlobalEffect(EffectChoice.Solid,new Rgb(0,0,80),1);
        controller.SetZones([new Zone([83,88,94],EffectChoice.Wave,new Rgb(255,0,0))]);
        var lighting=new Falchion75Lighting();lighting.Configure(controller.BuildLayers(true),DecorationMode.Custom,DecorationMode.Custom);
        var keys=new LedFrame(83);keys.Pixels.Fill(new Rgb(0,0,80));bool lit=false;
        for(int step=0;step<20;step++)
        {
            var result=lighting.Compose(keys,new EffectContext(TimeSpan.FromSeconds(step*.1),TimeSpan.FromSeconds(.1),[],Falchion75Layout.Profile));
            foreach(int i in new[]{83,88,94}){Assert.Equal(0,result[i].G);Assert.Equal(0,result[i].B);lit|=result[i].R>0;}
            Assert.All(result.Pixels[..83].ToArray(),p=>Assert.Equal(new Rgb(0,0,80),p));Assert.Equal(new Rgb(0,255,0),result[95]);
        }
        Assert.True(lit);
    }
    [Fact]
    public async Task StarlightZoneAlsoUsesItsChosenColour()
    {
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        controller.SetGlobalEffect(EffectChoice.Off,Rgb.Black,1);
        controller.SetZones([new Zone(Enumerable.Range(0,controller.Profile.LedCount).ToArray(),EffectChoice.Starlight,new Rgb(255,0,0))]);
        var comp=new Compositor(controller.Profile.LedCount);foreach(var l in controller.BuildLayers())comp.Add(l);
        var frame=new LedFrame(controller.Profile.LedCount);bool lit=false;
        for(int step=0;step<40;step++)
        {
            comp.Compose(frame,new EffectContext(TimeSpan.FromSeconds(step*.1),TimeSpan.FromSeconds(.1),[],controller.Profile));
            Assert.All(frame.Pixels.ToArray(),p=>{Assert.Equal(0,p.G);Assert.Equal(0,p.B);lit|=p.R>0;});
        }
        Assert.True(lit);
    }
    [Fact]
    public async Task GlobalPresetKeepsItsOwnRainbowPalette()
    {
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        controller.SetGlobalEffect(EffectChoice.Rainbow,new Rgb(255,0,0),1);
        var comp=new Compositor(controller.Profile.LedCount);foreach(var l in controller.BuildLayers())comp.Add(l);
        var frame=new LedFrame(controller.Profile.LedCount);comp.Compose(frame,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],controller.Profile));
        Assert.Contains(frame.Pixels.ToArray(),p=>p.G>0 || p.B>0);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AudioSpectrumUsesZoneColourWhileExplicitRainbowRemainsRainbow(bool rainbow)
    {
        var state=new SensorState();state.Apply(new RSS_II_RGB.Core.Ipc.SensorSample(RSS_II_RGB.Core.Ipc.SensorKind.AudioBands,Enumerable.Repeat(.8,32).ToArray(),0));
        await using var controller=new KeyboardController(new QuietLog(),state);
        controller.SetGlobalEffect(EffectChoice.Off,Rgb.Black,1);
        controller.SetZones([new Zone(Enumerable.Range(0,controller.Profile.LedCount).ToArray(),EffectChoice.Audio,new Rgb(255,0,0),rainbow?AudioZoneMode.SolidRainbow:AudioZoneMode.Spectrum)]);
        var comp=new Compositor(controller.Profile.LedCount);foreach(var l in controller.BuildLayers())comp.Add(l);
        var frame=new LedFrame(controller.Profile.LedCount);comp.Compose(frame,new EffectContext(TimeSpan.FromSeconds(1),TimeSpan.Zero,[],controller.Profile));
        if(rainbow)Assert.Contains(frame.Pixels.ToArray(),p=>p.G>0||p.B>0);
        else {Assert.Contains(frame.Pixels.ToArray(),p=>p.R>0);Assert.All(frame.Pixels.ToArray(),p=>{Assert.Equal(0,p.G);Assert.Equal(0,p.B);});}
    }
    private sealed class QuietLog:ILogSink {public void Log(LogLevel level,string message,Exception? exception=null){}}
}
