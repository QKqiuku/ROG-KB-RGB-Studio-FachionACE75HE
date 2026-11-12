using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Sensors;
namespace RSS_II_RGB.Core.Tests;
public class MusicShapeRefinementTests
{
    private static Rgb[] Draw(MusicKind kind,double time=.5,double volume=.3,double bass=.3)
    {
        var layer=new MusicLayer("shape",kind,new SensorState(),Rgb.White,KeyMask.All);
        var pixels=new Rgb[401*201];layer.RenderCanvas(pixels,401,201,TimeSpan.FromSeconds(time),new AudioFrame {Generation=1,Sequence=1,Volume=volume,Bass=bass,Spectrum=[.2,.3]},TestContext.Current.CancellationToken);
        return pixels;
    }
    [Fact]
    public void PrismUsesTwoDominantHuesInsteadOfAFullRainbowOn83Keys()
    {
        var layer=new MusicLayer("palette",MusicKind.RhythmOrbit,new SensorState(),Rgb.White,KeyMask.All);
        var pixels=new Rgb[83];
        layer.Render(pixels,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile),new AudioFrame {Generation=1,Volume=.3,Bass=.3,Spectrum=[.1,.8,.2]});
        static double Hue(Rgb p)
        {
            double max=Math.Max(p.R,Math.Max(p.G,p.B)),min=Math.Min(p.R,Math.Min(p.G,p.B)),delta=max-min;
            double h=delta==0?0:(max==p.R?(p.G-p.B)/delta:max==p.G?(p.B-p.R)/delta+2:(p.R-p.G)/delta+4)/6;
            return h-Math.Floor(h);
        }
        static bool Near(double h,double target)=>Math.Min(Math.Abs(h-target),1-Math.Abs(h-target))<.012;
        var hues=pixels.Select(Hue).ToArray();
        Assert.True(hues.Count(h=>Near(h,0)||Near(h,.20))>=60);
        Assert.True(hues.Count(h=>Near(h,0))>=10);Assert.True(hues.Count(h=>Near(h,.20))>=10);
        var first=pixels.ToArray();
        layer.Render(pixels,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile),new AudioFrame {Generation=1,Volume=.3,Bass=.3,Spectrum=[.8,.1,.9]});
        Assert.Equal(first,pixels);
    }
    [Fact]
    public void PrismFillsTheBoardWithBrightColourEvenBetweenBeats()
    {
        var pixels=Draw(MusicKind.RhythmOrbit,volume:.3,bass:.1);
        Assert.All(pixels,p=>Assert.True(Math.Max(p.R,Math.Max(p.G,p.B))>=80));
        Assert.True(pixels.Select(p=>(p.R/32,p.G/32,p.B/32)).Distinct().Count()>20);
    }
    [Fact]
    public void PrismBassBeatTurnsTheFullColourField()
    {
        var layer=new MusicLayer("sweep",MusicKind.RhythmOrbit,new SensorState(),Rgb.White,KeyMask.All);
        var quiet=new Rgb[401*201];var beat=new Rgb[401*201];
        var audio=new AudioFrame {Generation=1,Volume=.3,Bass=.3};
        layer.RenderCanvas(quiet,401,201,TimeSpan.Zero,audio,TestContext.Current.CancellationToken);
        layer.RenderCanvas(beat,401,201,TimeSpan.Zero,audio with {BassBeatId=1,BassOnset=.8},TestContext.Current.CancellationToken);
        Assert.True(quiet.Zip(beat).Count(p=>Math.Abs(p.First.R-p.Second.R)+Math.Abs(p.First.G-p.Second.G)+Math.Abs(p.First.B-p.Second.B)>40)>quiet.Length/8);
    }
    [Fact]
    public void PrismBeatChangesRemainVisibleOnThe83KeyLayout()
    {
        var layer=new MusicLayer("keys",MusicKind.RhythmOrbit,new SensorState(),Rgb.White,KeyMask.All);
        var quiet=new Rgb[83];var loud=new Rgb[83];
        var ctx=new EffectContext(TimeSpan.FromSeconds(.18),TimeSpan.FromMilliseconds(16),[],Falchion75Layout.Profile);
        var audio=new AudioFrame {Generation=1,Volume=.3,Bass=.3};
        layer.Render(quiet,ctx,audio);
        var beat=audio with {BassBeatId=1,BassOnset=.8};
        layer.Render(loud,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile),beat);
        layer.Render(loud,ctx,beat);
        Assert.True(quiet.Zip(loud).Count(p=>Math.Abs(p.First.R-p.Second.R)+Math.Abs(p.First.G-p.Second.G)+Math.Abs(p.First.B-p.Second.B)>50)>=20);
    }
    [Fact]
    public void PrismBassChangesTheColourFacetAtFixedTime()
    {
        var quiet=Draw(MusicKind.RhythmOrbit,bass:.001);
        var loud=Draw(MusicKind.RhythmOrbit,bass:.8);
        var a=quiet[100*401+300];var b=loud[100*401+300];
        Assert.NotEqual(a,b);
    }
    [Fact]
    public void PrismBassChangesFacetGeometryBetweenBeats()
    {
        var quiet=Draw(MusicKind.RhythmOrbit,bass:.001);
        var loud=Draw(MusicKind.RhythmOrbit,bass:.8);
        Assert.False(quiet.SequenceEqual(loud));
    }
    [Fact]
    public void EnergyCoreIsCircularInPixelsAndPulsesWithAudio()
    {
        var pixels=Draw(MusicKind.EnergyCore);
        static byte Peak(Rgb p)=>Math.Max(p.R,Math.Max(p.G,p.B));
        double threshold=Peak(pixels[100*401+200])*.6;
        int right=200,bottom=100;
        while(right<400 && Peak(pixels[100*401+right])>=threshold)right++;
        while(bottom<200 && Peak(pixels[bottom*401+200])>=threshold)bottom++;
        int horizontal=(right-200)*2,vertical=(bottom-100)*2;
        Assert.InRange(Math.Abs(horizontal-vertical),0,3);
        Assert.True(horizontal>100);
        Assert.False(pixels.SequenceEqual(Draw(MusicKind.EnergyCore,volume:.05)));
    }
    [Fact]
    public void PrismKeepsMovingColoursWithoutBlackHoles()
    {
        var pixels=Draw(MusicKind.RhythmOrbit);
        Assert.True(Math.Max(pixels[100*401+200].R,Math.Max(pixels[100*401+200].G,pixels[100*401+200].B))>=32);
        Assert.False(pixels.SequenceEqual(Draw(MusicKind.RhythmOrbit,2)));
        foreach(int index in new[]{0,400,200*401,201*401-1})Assert.True(Math.Max(pixels[index].R,Math.Max(pixels[index].G,pixels[index].B))>=24);
    }
}
