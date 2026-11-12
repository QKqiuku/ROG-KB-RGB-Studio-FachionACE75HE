using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class SineSectorsReferenceTests
{
    [Fact]
    public void RainbowModeMatchesTheFormerMulticolourEffect()
    {
        var bytes=Render(PatternKind.SineSectors,1.25,new PatternOptions{Rainbow=true}).SelectMany(p=>new[]{p.R,p.G,p.B}).ToArray();
        Assert.Equal("CEA7CA7DBBEB36016396E3414A953515D005DA95A77152C6EF68342AC0B2593A",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DarkSectorsKeepTheReferenceGlow(bool rainbow)
    {
        var frames = Enumerable.Range(0, 32).SelectMany(n => Render(PatternKind.SineSectors, n * .25,new PatternOptions{Rainbow=rainbow})).ToArray();
        Assert.InRange(frames.Min(p => Math.Max(p.R, Math.Max(p.G, p.B))), 14, 17);
        Assert.Contains(frames, p => Math.Max(p.R, Math.Max(p.G, p.B)) <= 17);
        Assert.Contains(frames, p => Math.Max(p.R, Math.Max(p.G, p.B)) >= 250);
    }

    [Fact]
    public void SingleColorUsesTheSelectedHueAcrossTheBoard()
    {
        foreach (var p in Render(PatternKind.SineSectors, .4,new PatternOptions{Rainbow=false}))
        {
            Assert.Equal(0,p.G);
            Assert.Equal(0,p.B);
        }
    }

    [Fact]
    public void ControlsStillAffectTheProceduralField()
    {
        var defaults = new PatternOptions();
        Assert.Equal(Render(PatternKind.SineSectors, 1), Render(PatternKind.SineSectors, .5, defaults with { Speed = 2 }));
        Assert.False(Render(PatternKind.SineSectors, 1).SequenceEqual(Render(PatternKind.SineSectors, 1, defaults with { Width = 2 })));
        Assert.False(Render(PatternKind.SineSectors, 1).SequenceEqual(Render(PatternKind.SineSectors, 1, defaults with { Direction = 90 })));
        Assert.All(Render(PatternKind.SineSectors, 1, defaults with { Saturation = 0 }), p => Assert.Equal(p.R, p.G));
        Assert.All(Render(PatternKind.SineSectors, 1, defaults with { Rainbow = false }), p => { Assert.Equal(0, p.G); Assert.Equal(0, p.B); });
    }

    private static Rgb[] Render(PatternKind kind, double seconds, PatternOptions? options = null)
    {
        var layout = Falchion75Layout.Profile;
        var layer = new PatternLayer("sine", kind, new Rgb(255, 0, 0), KeyMask.All, options ?? new());
        var pixels = new Rgb[layout.LedCount];
        layer.Render(pixels, new EffectContext(TimeSpan.FromSeconds(seconds), TimeSpan.FromMilliseconds(16), [], layout));
        return pixels;
    }
}
