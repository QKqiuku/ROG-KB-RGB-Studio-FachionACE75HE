using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;

namespace RSS_II_RGB.Core.Tests;

public class MusicSurfaceTests
{
    private const int Width=401,Height=201;
    private static readonly AudioFrame Audio=new() {Generation=1,Sequence=1,Volume=.3,Bass=.3,Spectrum=[.02,.1,.3,.5]};
    private static MusicLayer Layer(MusicKind kind,PatternOptions? options=null)=>
        new("surface",kind,new SensorState(),new Rgb(230,50,80),KeyMask.All,options);
    private static Rgb[] Draw(MusicLayer layer,double time,AudioFrame? audio=null)
    {
        var pixels=new Rgb[Width*Height];
        layer.RenderCanvas(pixels,Width,Height,TimeSpan.FromSeconds(time),audio??Audio,TestContext.Current.CancellationToken);
        return pixels;
    }
    private static byte Peak(Rgb p)=>Math.Max(p.R,Math.Max(p.G,p.B));

    [Fact]
    public void SolarRainbowChangesColourAtTheStationaryCentre()
    {
        var layer=Layer(MusicKind.EnergyCore);
        var a=Draw(layer,0);var b=Draw(layer,1);
        Assert.NotEqual(a[100*Width+200],b[100*Width+200]);
    }

    [Theory]
    [InlineData(MusicKind.EnergyCore)]
    [InlineData(MusicKind.RhythmOrbit)]
    public void SingleColourHasAColourGradientBeyondBrightness(MusicKind kind)
    {
        var pixels=Draw(Layer(kind,new PatternOptions {Rainbow=false}),0);
        var lit=pixels.Where(p=>Peak(p)>32 && p.R>32).ToArray();
        var green=lit.Select(p=>(double)p.G/p.R).ToArray();
        var blue=lit.Select(p=>(double)p.B/p.R).ToArray();
        Assert.True(green.Max()-green.Min()+blue.Max()-blue.Min()>.25);
    }
    [Fact]
    public void NeonHasBroadBandsThatTranslateHorizontally()
    {
        var f=Audio with {Spectrum=[.5]};
        var first=Draw(Layer(MusicKind.NeonSpectrum),0,f);
        var next=Draw(Layer(MusicKind.NeonSpectrum),.35,f);
        // A 0.2-board shift matches the same colour after 0.35 seconds.
        for(int x=0;x<320;x++)
        {
            var a=first[(Height-1)*Width+x];var b=next[(Height-1)*Width+x+80];
            Assert.InRange(Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B),0,3);
        }
        var left=first[(Height-1)*Width];var near=first[(Height-1)*Width+40];
        Assert.InRange(Math.Abs(left.R-near.R)+Math.Abs(left.G-near.G)+Math.Abs(left.B-near.B),0,40);
    }

    [Fact]
    public void NeonUsesSpectrumFillAndBrightnessWithFlowingSaturatedColour()
    {
        var bars=Draw(Layer(MusicKind.SpectrumBars),0);
        var neon=Layer(MusicKind.NeonSpectrum);
        var first=Draw(neon,0);
        Assert.Equal(bars.Select(Peak),first.Select(Peak));
        Assert.False(first.SequenceEqual(bars));
        Assert.False(first.SequenceEqual(Draw(neon,1)));
        var bottom=first[^1];
        Assert.True(Math.Min(bottom.R,Math.Min(bottom.G,bottom.B))<=Peak(bottom)*.2);
    }

    [Fact]
    public void NeonCustomColourPreservesSpectrumGeometryAndColour()
    {
        var options=new PatternOptions {Rainbow=false,Saturation=.8};
        Assert.Equal(Draw(Layer(MusicKind.SpectrumBars,options),0),Draw(Layer(MusicKind.NeonSpectrum,options),0));
    }

    [Fact]
    public void FireBottomStaysWarmAndSaturated()
    {
        var pixels=Draw(Layer(MusicKind.RhythmFire),0);
        foreach(var p in pixels.Skip((Height-1)*Width))
        {
            Assert.True(p.R>50);
            Assert.Equal(0,p.B);
        }
    }

    [Fact]
    public void CoreSpikesGrowWithBassAndHaveAngularGapsInsteadOfRings()
    {
        var quiet=Draw(Layer(MusicKind.EnergyCore),0,Audio with {Bass=.001,Spectrum=[0]});
        var loud=Draw(Layer(MusicKind.EnergyCore),0,Audio with {Bass=.8,Spectrum=[0]});
        int Reach(Rgb[] pixels)=>Enumerable.Range(200,201).Last(x=>Peak(pixels[100*Width+x])>20)-200;
        Assert.True(Reach(loud)>Reach(quiet)+55);
        // Equal-radius points alternate between attached spikes and dark gaps.
        var levels=Enumerable.Range(0,64).Select(i=>
        {
            double a=i*Math.PI*2/64;
            int x=(int)Math.Round(200+110*Math.Cos(a)),y=(int)Math.Round(100+110*Math.Sin(a));
            return y<0||y>=Height ? (byte)0 : Peak(loud[y*Width+x]);
        }).ToArray();
        Assert.Contains(levels,p=>p>20);Assert.Contains(levels,p=>p<5);
    }

    [Fact]
    public void CoreKeepsFilledCentreAndHasAttachedSpikesThatClearInSilence()
    {
        var layer=Layer(MusicKind.EnergyCore);
        var first=Draw(layer,0);
        Assert.True(Peak(first[(Height/2)*Width+Width/2])>60);
        Assert.Contains(Enumerable.Range(0,140),x=>Peak(first[(Height/2)*Width+x])>16);
        Assert.False(first.SequenceEqual(Draw(layer,.4)));
        Assert.All(Draw(layer,3,AudioFrame.Empty with {Generation=1,Sequence=2}),p=>Assert.Equal(Rgb.Black,p));
    }
}
