using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class PreviewPatternTests
{
    // Observations from the public Lumo previews, not its private implementation.
    [Theory]
    [InlineData(PatternKind.Silk)]
    [InlineData(PatternKind.Marble)]
    public void PastelMaterialsKeepBrightLowSaturationHighlights(PatternKind kind)
    {
        var pixels = Render(kind, .63);
        Assert.True(pixels.Average(p => Math.Max(p.R, Math.Max(p.G, p.B))) > (kind == PatternKind.Marble ? 150 : 220));
        Assert.Contains(pixels, p => Math.Min(p.R, Math.Min(p.G, p.B)) > 110);
        if (kind == PatternKind.Silk) Assert.True(pixels.Average(p => (int)p.R-p.B)>35);
    }

    [Fact]
    public void WipeReplacesColoursWithoutLeavingDarkHalfBoards()
    {
        foreach (double t in new[] {0, .4, 1.2, 2.7})
            Assert.All(Render(PatternKind.Wipe,t), p => Assert.True(Math.Max(p.R,Math.Max(p.G,p.B)) >= 185));
    }

    [Fact]
    public void MeteorHasSeveralIndependentRowsAndPastelTails()
    {
        var pixels=Render(PatternKind.Meteor,.63);
        Assert.Contains(pixels,p=> Math.Max(p.R,Math.Max(p.G,p.B))>40 && Math.Min(p.R,Math.Min(p.G,p.B))>10);
        int litRows=Enumerable.Range(0,pixels.Length).Where(i=>pixels[i].R+pixels[i].G+pixels[i].B>40).Select(i=>Falchion75Layout.Profile.ByIndex(i).Row).Distinct().Count();
        Assert.True(litRows>=3);
    }

    [Fact]
    public void HeartbeatHasDarkRestAndUniformRedDoublePulse()
    {
        Assert.All(Render(PatternKind.Heartbeat,0),p=>Assert.Equal(Rgb.Black,p));
        foreach(double t in new[]{.33,.55})
        {
            var pixels=Render(PatternKind.Heartbeat,t);
            Assert.All(pixels,p=>Assert.Equal(pixels[0],p));
            Assert.True(pixels[0].R>80 && pixels[0].G<10);
        }
    }

    [Fact]
    public void NeonPulseKeepsTheBluePurpleBackgroundLit()
    {
        Assert.All(Render(PatternKind.NeonPulse,.63),p=>Assert.True(p.B>150));
    }

    [Theory]
    [InlineData(PatternKind.Marble)]
    [InlineData(PatternKind.Silk)]
    [InlineData(PatternKind.NeonPulse)]
    [InlineData(PatternKind.Wipe)]
    public void SelectingOneColourStillAnimates(PatternKind kind)
    {
        var profile=Falchion75Layout.Profile;
        var layer=new PatternLayer("mono",kind,new Rgb(255,0,0),KeyMask.All,new PatternOptions { Rainbow=false });
        var frames=new List<Rgb[]>();
        foreach(double time in new[]{.1,.63,1.31})
        {
            var pixels=new Rgb[profile.LedCount];
            layer.Render(pixels,new EffectContext(TimeSpan.FromSeconds(time),TimeSpan.Zero,[],profile));
            Assert.All(pixels,p=>Assert.True(p.G==0 && p.B==0));
            frames.Add(pixels);
        }
        Assert.Contains(frames.Skip(1),f=>!f.SequenceEqual(frames[0]));
    }

    [Fact]
    public void RaindropWidthChangesItsTrailAndSplash()
    {
        var profile=Falchion75Layout.Profile;
        Rgb[] Sample(double width)
        {
            var pixels=new Rgb[profile.LedCount];
            new PatternLayer("rain",PatternKind.RainDrops,Rgb.White,KeyMask.All,new PatternOptions { Width=width })
                .Render(pixels,new EffectContext(TimeSpan.FromSeconds(.63),TimeSpan.Zero,[],profile));
            return pixels;
        }
        Assert.False(Sample(.25).SequenceEqual(Sample(3)));
    }

    [Fact]
    public void NebulaIsABrightBluePinkCloudRatherThanDarkPurpleStripes()
    {
        Assert.True(Render(PatternKind.Nebula,.63).Average(p=>Math.Max(p.R,Math.Max(p.G,p.B)))>240);
    }

    private static Rgb[] Render(PatternKind kind,double time)
    {
        var profile=Falchion75Layout.Profile;
        var pixels=new Rgb[profile.LedCount];
        new PatternLayer("preview",kind,Rgb.White,KeyMask.All,new PatternOptions())
            .Render(pixels,new EffectContext(TimeSpan.FromSeconds(time),TimeSpan.Zero,[],profile));
        return pixels;
    }
}
