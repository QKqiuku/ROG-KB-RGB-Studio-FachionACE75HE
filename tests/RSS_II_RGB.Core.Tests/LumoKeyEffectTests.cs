using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class LumoKeyEffectTests
{
    [Theory]
    [InlineData(KeyEffectKind.Spotlight)]
    [InlineData(KeyEffectKind.Lightning)]
    [InlineData(KeyEffectKind.Fireworks)]
    public void LitAccentsAddToTheBaseInsteadOfReplacingItsChannels(KeyEffectKind kind)
    {
        var profile=Falchion75Layout.Profile;
        var layer=new ReactivePatternLayer("accent",kind,new Rgb(200,0,0),KeyMask.All,new PatternOptions {Rainbow=false},1);
        var comp=new Compositor(83);
        comp.Add(new PatternLayer("base",PatternKind.Solid,new Rgb(0,40,100),KeyMask.All,new PatternOptions {Rainbow=false}));comp.Add(layer);
        var frame=new LedFrame(83);var hits=new[]{new KeyHit(profile.IndexForKeyName("G"),TimeSpan.Zero)};
        comp.Compose(frame,new EffectContext(TimeSpan.FromSeconds(.3),TimeSpan.FromSeconds(.016),hits,profile));
        Assert.Contains(frame.Pixels.ToArray(),p=>p.R>0);
        Assert.All(frame.Pixels.ToArray(),p=>{Assert.Equal(40,p.G);Assert.Equal(100,p.B);});
        comp.Compose(frame,new EffectContext(TimeSpan.FromSeconds(4),TimeSpan.FromSeconds(.016),[],profile));
        Assert.All(frame.Pixels.ToArray(),p=>Assert.Equal(new Rgb(0,40,100),p));
    }
    [Fact]
    public void RainbowRippleProducesSeveralHuesWhileSolidKeepsTheChosenColour()
    {
        var hit=new KeyHit(48,TimeSpan.Zero);
        var solid=Render(new ReactivePatternLayer("solid",KeyEffectKind.Ripple,new Rgb(255,0,0),KeyMask.All,new PatternOptions{Rainbow=false}),.25,hit);
        Assert.Contains(solid,p=>p.R>0);Assert.All(solid,p=>{Assert.Equal(0,p.G);Assert.Equal(0,p.B);});
        var rainbow=Render(new ReactivePatternLayer("rainbow",KeyEffectKind.Ripple,new Rgb(255,0,0),KeyMask.All,new PatternOptions{ReactiveRainbow=true}),.25,hit);
        var dominant=rainbow.Where(p=>Math.Max(p.R,Math.Max(p.G,p.B))>30).Select(p=>p.R>=p.G&&p.R>=p.B?0:p.G>=p.B?1:2).Distinct().Count();
        Assert.True(dominant>=2);
    }

    [Fact]
    public void KeyFireworksUsesTheSharedRocketAndParticleBloom()
    {
        var profile=Falchion75Layout.Profile;
        int origin=profile.IndexForKeyName("G");
        var layer=new ReactivePatternLayer("firework",KeyEffectKind.Fireworks,new Rgb(255,0,0),KeyMask.All,new PatternOptions{Rainbow=false});
        double age=.65;
        var pixels=Render(layer,age,new KeyHit(origin,TimeSpan.Zero));
        foreach(var key in profile.Keys)
        {
            var source=profile.ByIndex(origin);
            double glow=FireworkBurst.Light(key.Col/14.0,key.Row/5.0,source.Col/14.0,source.Row/5.0,age,1,.3,origin);
            Assert.Equal(new Rgb(255,0,0).Scale((float)glow),pixels[key.Index]);
        }
    }

    public static TheoryData<KeyEffectKind> Transients => new(Enum.GetValues<KeyEffectKind>().Where(k=>k is not KeyEffectKind.Dye and not KeyEffectKind.DarkRipple));
    private static Rgb[] Render(ReactivePatternLayer layer,double t,params KeyHit[] hits)
    {
        var pixels=new Rgb[Falchion75Layout.Profile.LedCount];
        layer.Render(pixels,new EffectContext(TimeSpan.FromSeconds(t),TimeSpan.FromSeconds(.05),hits,Falchion75Layout.Profile));return pixels;
    }
    [Theory]
    [MemberData(nameof(Transients))]
    public void KeyEffectWaitsForRealHitAnimatesAndExpires(KeyEffectKind kind)
    {
        var layer=new ReactivePatternLayer("key",kind,Rgb.White,KeyMask.All,new PatternOptions());
        Assert.All(Render(layer,0),p=>Assert.Equal(Rgb.Black,p));
        Render(layer,.1,new KeyHit(48,TimeSpan.FromSeconds(.1)));
        var a=Render(layer,.2);var b=Render(layer,.5);
        Assert.Contains(a,p=>p!=Rgb.Black);
        Assert.False(a.SequenceEqual(b));
        Assert.All(Render(layer,4),p=>Assert.Equal(Rgb.Black,p));
    }
    [Fact]
    public void DarkRippleMultipliesTheBaseAndNeverAddsLight()
    {
        var layer=new ReactivePatternLayer("dark",KeyEffectKind.DarkRipple,Rgb.White,KeyMask.All,new PatternOptions());
        Assert.Equal(BlendMode.Multiply,layer.Blend);
        Assert.All(Render(layer,0),p=>Assert.Equal(Rgb.White,p));
        Render(layer,.1,new KeyHit(48,TimeSpan.FromSeconds(.1)));
        Assert.Contains(Render(layer,.3),p=>p.R<100 && p.R==p.G && p.G==p.B);
        Assert.All(Render(layer,4),p=>Assert.Equal(Rgb.White,p));
    }
    [Fact]
    public void DyeStaysAtItsNewColourAfterTheWaveFinishes()
    {
        var layer=new ReactivePatternLayer("dye",KeyEffectKind.Dye,Rgb.White,KeyMask.All,new PatternOptions());
        var idle=Render(layer,0);
        Render(layer,.1,new KeyHit(48,TimeSpan.FromSeconds(.1)));
        var settled=Render(layer,5);
        Assert.False(idle.SequenceEqual(settled));
        Assert.All(settled,p=>Assert.Equal(settled[0],p));
        Assert.Equal(settled,Render(layer,10));
        Render(layer,10.1,new KeyHit(57,TimeSpan.FromSeconds(10.1)));
        Assert.False(settled.SequenceEqual(Render(layer,15)));
    }
    [Theory]
    [MemberData(nameof(Transients))]
    public void HitsOutsideTheZoneAreIgnored(KeyEffectKind kind)
    {
        var layer=new ReactivePatternLayer("zone",kind,Rgb.White,KeyMask.FromIndices([46]),new PatternOptions());
        Render(layer,0,new KeyHit(48,TimeSpan.Zero),new KeyHit(-1,TimeSpan.Zero));
        Assert.All(Render(layer,.3),p=>Assert.Equal(Rgb.Black,p));
    }
    [Fact]
    public void SelectedColourAndSpeedAffectActualReactiveOutput()
    {
        var normal=new ReactivePatternLayer("a",KeyEffectKind.DoubleRing,new Rgb(255,0,0),KeyMask.All,new PatternOptions{Rainbow=false});
        var fast=new ReactivePatternLayer("b",KeyEffectKind.DoubleRing,new Rgb(255,0,0),KeyMask.All,new PatternOptions{Rainbow=false,Speed=2});
        Render(normal,0,new KeyHit(48,TimeSpan.Zero));Render(fast,0,new KeyHit(48,TimeSpan.Zero));
        var pixels=Render(normal,.4);Assert.Equal(pixels,Render(fast,.2));
        Assert.All(pixels,p=>Assert.True(p.G==0 && p.B==0));
    }
    [Fact]
    public void FloodOfHitsIsBoundedAndAllTransientStateExpires()
    {
        var layer=new ReactivePatternLayer("many",KeyEffectKind.Fireworks,Rgb.White,KeyMask.All,new PatternOptions());
        Render(layer,0,Enumerable.Range(0,1000).Select(i=>new KeyHit(i%83,TimeSpan.Zero)).ToArray());
        Assert.Contains(Render(layer,.2),p=>p!=Rgb.Black);
        Assert.All(Render(layer,5),p=>Assert.Equal(Rgb.Black,p));
    }
    [Fact]
    public void ZeroIntensityAlsoHidesTheDyeWaveEdge()
    {
        var layer=new ReactivePatternLayer("off",KeyEffectKind.Dye,Rgb.White,KeyMask.All,new PatternOptions(),brightness:0);
        Render(layer,0,new KeyHit(48,TimeSpan.Zero));
        foreach(double t in new[]{0,.1,.3,.6,1.1}) Assert.All(Render(layer,t),p=>Assert.Equal(Rgb.Black,p));
    }
    [Fact]
    public void HeartRangeChangesTheInteractiveShape()
    {
        Rgb[] Sample(double width)
        {
            var layer=new ReactivePatternLayer("heart",KeyEffectKind.Heart,Rgb.White,KeyMask.All,new PatternOptions {Width=width});
            Render(layer,0,new KeyHit(48,TimeSpan.Zero));return Render(layer,.4);
        }
        Assert.False(Sample(.25).SequenceEqual(Sample(3)));
    }

}
