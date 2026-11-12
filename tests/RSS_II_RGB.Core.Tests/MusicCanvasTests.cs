using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
namespace RSS_II_RGB.Core.Tests;
public class MusicCanvasTests
{
    [Fact]
    public void CanvasHasContinuousHeightInsteadOfSixKeyboardRows()
    {
        var layer=new MusicLayer("canvas",MusicKind.SpectrumBars,new SensorState(),Rgb.White,KeyMask.All);
        var image=new Rgb[655*240];
        var frame=new AudioFrame {Generation=1,Sequence=1,Volume=.2,Spectrum=[.05,.15,.35,.7]};
        layer.RenderCanvas(image,655,240,TimeSpan.Zero,frame,TestContext.Current.CancellationToken);
        int top=Enumerable.Range(0,240).First(y=>image[y*655].R>0);
        Assert.True(top%40!=0);
        Assert.True(image.Count(p=>p!=Rgb.Black)>83);
        Assert.NotEqual(image[top*655],image[(top+10)*655]);
    }
    [Fact]
    public void KeyboardAndCanvasSampleTheSameEffectField()
    {
        int width=19,height=6;
        var profile=new KeyboardProfile("grid",height,width,Enumerable.Range(0,width*height).Select(i=>new LedKey(0,"",i/width,i%width,i)).ToArray());
        var f=new AudioFrame {Generation=1,Sequence=1,Volume=.3,Spectrum=[.1,.3,.8],Waveform=[-.5,0,.5],BeatId=1,BassBeatId=1,LowBeatId=1,Onset=.5};
        foreach(var kind in new[]{MusicKind.SpectrumBars,MusicKind.EnergyCore,MusicKind.SoundWave,MusicKind.Shockwave,MusicKind.RhythmOrbit})
        {
            var a=new MusicLayer("keys",kind,new SensorState(),Rgb.White,KeyMask.All);
            var b=new MusicLayer("canvas",kind,new SensorState(),Rgb.White,KeyMask.All);
            var keys=new Rgb[width*height];var pixels=new Rgb[keys.Length];
            a.Render(keys,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],profile),f);
            b.RenderCanvas(pixels,width,height,TimeSpan.Zero,f,TestContext.Current.CancellationToken);
            Assert.Equal(keys,pixels);
        }
    }
    [Fact]
    public void SilenceAndInvalidDimensionsCannotCreateAFrame()
    {
        var layer=new MusicLayer("canvas",MusicKind.SpectrumBars,new SensorState(),Rgb.White,KeyMask.All);
        var frame=new Rgb[655*240];layer.RenderCanvas(frame,655,240,TimeSpan.Zero,AudioFrame.Empty,TestContext.Current.CancellationToken);
        Assert.All(frame,p=>Assert.Equal(Rgb.Black,p));
        Assert.Throws<ArgumentOutOfRangeException>(()=>layer.RenderCanvas(new Rgb[10],655,240,TimeSpan.Zero,AudioFrame.Empty,TestContext.Current.CancellationToken));
    }
}
