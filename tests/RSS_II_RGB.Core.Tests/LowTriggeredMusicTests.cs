using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
namespace RSS_II_RGB.Core.Tests;
public class LowTriggeredMusicTests
{
    [Theory]
    [InlineData(MusicKind.Shockwave)]
    [InlineData(MusicKind.BeatFlash)]
    public void GeneralHighFrequencyBeatCannotTriggerLowEffects(MusicKind kind)
    {
        var layer=new MusicLayer("filtered",kind,new SensorState(),Rgb.White,KeyMask.All);
        var image=new Rgb[401*201];
        var high=new AudioFrame {Generation=1,Volume=.4,BeatId=1,Onset=.8,Treble=.8};
        layer.RenderCanvas(image,401,201,TimeSpan.Zero,high,TestContext.Current.CancellationToken);
        Assert.All(image,p=>Assert.Equal(Rgb.Black,p));
        var low=high with {Bass=.3,BassBeatId=1,LowBeatId=1,BassOnset=.6,LowOnset=.6};
        layer.RenderCanvas(image,401,201,TimeSpan.FromSeconds(.1),low,TestContext.Current.CancellationToken);
        Assert.Contains(image,p=>p!=Rgb.Black);
    }
    [Fact]
    public void ShockwaveAcceptsEveryBassHitAndResetsOnSourceChange()
    {
        var layer=new MusicLayer("spaced",MusicKind.Shockwave,new SensorState(),Rgb.White,KeyMask.All);
        var pixels=new Rgb[40*20];
        for(int i=0;i<5;i++)
            layer.RenderCanvas(pixels,40,20,TimeSpan.FromSeconds(i*.2),new AudioFrame {Generation=1,Volume=.4,Bass=.3,BassOnset=.6,BassBeatId=i+1},TestContext.Current.CancellationToken);
        Assert.Equal(5,layer.ActivePulseCount);
        layer.RenderCanvas(pixels,40,20,TimeSpan.FromSeconds(.9),new AudioFrame {Generation=2,Volume=.4,Bass=.3,BassOnset=.6,BassBeatId=1},TestContext.Current.CancellationToken);
        Assert.Equal(1,layer.ActivePulseCount);
    }
    [Fact]
    public void ShockwaveIsARoundExpandingRingInPixelSpace()
    {
        var layer=new MusicLayer("ring",MusicKind.Shockwave,new SensorState(),Rgb.White,KeyMask.All);
        var image=new Rgb[401*201];
        var f=new AudioFrame {Generation=1,Volume=.4,Bass=.3,BassBeatId=1,BassOnset=.6};
        layer.RenderCanvas(image,401,201,TimeSpan.Zero,f,TestContext.Current.CancellationToken);
        layer.RenderCanvas(image,401,201,TimeSpan.FromSeconds(.1),f,TestContext.Current.CancellationToken);
        bool Lit(Rgb p)=>Math.Max(p.R,Math.Max(p.G,p.B))>32;
        var xs=Enumerable.Range(0,401).Where(x=>Lit(image[100*401+x])).ToArray();
        var ys=Enumerable.Range(0,201).Where(y=>Lit(image[y*401+200])).ToArray();
        Assert.NotEmpty(xs);Assert.NotEmpty(ys);
        Assert.True(xs.Max()-xs.Min()>130);
        Assert.InRange(Math.Abs((xs.Max()-xs.Min())-(ys.Max()-ys.Min())),0,2);
    }
}
