using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class PatternLayerTests
{
    public static TheoryData<PatternKind> AnimatedPatterns => new(
        Enum.GetValues<PatternKind>().Where(k => k is not PatternKind.Solid and not PatternKind.PressFade
            and not PatternKind.Ripple));

    [Theory]
    [MemberData(nameof(AnimatedPatterns))]
    public void EveryAnimationProducesLightAndChangesOverTime(PatternKind kind)
    {
        var layer = new PatternLayer("test", kind, new Rgb(255, 30, 10), KeyMask.All, new PatternOptions());
        var frames = new List<Rgb[]>();
        for (int n = 0; n < 12; n++) frames.Add(Render(layer, n * 0.37));
        Assert.Contains(frames.SelectMany(f => f), p => p != Rgb.Black);
        Assert.Contains(frames.Skip(1), f => !f.SequenceEqual(frames[0]));
    }

    [Theory]
    [InlineData(PatternKind.PressFade)]
    [InlineData(PatternKind.Ripple)]
    public void ReactivePresetsWaitForAKeyAndExpire(PatternKind kind)
    {
        var layer = new PatternLayer("test", kind, new Rgb(255, 0, 0), KeyMask.All,
            new PatternOptions { Rainbow = false });
        Assert.All(Render(layer, 0), p => Assert.Equal(Rgb.Black, p));
        var lit = Render(layer, 0.1, new KeyHit(46, TimeSpan.FromSeconds(0.1)));
        Assert.Equal(new Rgb(255, 0, 0), lit[46]);
        Assert.All(Render(layer, 10), p => Assert.Equal(Rgb.Black, p));
    }

    [Fact]
    public void SpeedScalesTimeAndDirectionChangesWaveGeometry()
    {
        var normal = new PatternLayer("a", PatternKind.Wave, Rgb.White, KeyMask.All, new PatternOptions());
        var fast = new PatternLayer("b", PatternKind.Wave, Rgb.White, KeyMask.All, new PatternOptions { Speed = 2 });
        Assert.Equal(Render(normal, 2), Render(fast, 1));
        var vertical = new PatternLayer("c", PatternKind.Wave, Rgb.White, KeyMask.All,
            new PatternOptions { Direction = 90 });
        Assert.False(Render(normal, 1).SequenceEqual(Render(vertical, 1)));
    }

    [Fact]
    public void MonochromeAndInvalidParametersRemainUsable()
    {
        var layer = new PatternLayer("test", PatternKind.SineWave, new Rgb(255, 0, 0), KeyMask.All,
            new PatternOptions { Speed = double.NaN, Width = 0, Density = double.PositiveInfinity,
                Direction = double.NaN, Saturation = -1, Rainbow = false });
        var pixels = Render(layer, 1);
        Assert.Contains(pixels, p => p.R > 0);
        Assert.All(pixels, p => { Assert.Equal(0, p.G); Assert.Equal(0, p.B); });
    }

    [Theory]
    [InlineData(PatternKind.Wave)]
    [InlineData(PatternKind.Rainbow)]
    public void MonochromeWavesStillMove(PatternKind kind)
    {
        var layer = new PatternLayer("wave", kind, new Rgb(255, 0, 0), KeyMask.All, new PatternOptions { Rainbow = false });
        var first = Render(layer, 0.1);
        var next = Render(layer, 0.8);
        Assert.False(first.SequenceEqual(next));
        Assert.True(first.Select(p => p.R).Distinct().Count() > 2);
        Assert.All(next, p => { Assert.Equal(0, p.G); Assert.Equal(0, p.B); });
    }

    [Fact]
    public void CountdownStartsAtTenEvenWhenTheEngineHasBeenRunningForAWhile()
    {
        var first = new PatternLayer("first", PatternKind.CountdownFireworks, Rgb.White, KeyMask.All, new PatternOptions());
        var later = new PatternLayer("later", PatternKind.CountdownFireworks, Rgb.White, KeyMask.All, new PatternOptions());
        Assert.Equal(Render(first, 0), Render(later, 100));
        Assert.False(Render(later, 101).SequenceEqual(Render(first, 0)));
    }

    [Theory]
    [InlineData(PatternKind.Ripple)]
    public void ReactiveZonesIgnoreKeysOutsideTheirMask(PatternKind kind)
    {
        var layer = new PatternLayer("zone", kind, Rgb.White, KeyMask.FromIndices([48]), new PatternOptions());
        Render(layer, 0, new KeyHit(46, TimeSpan.Zero));
        foreach (double time in new[] { 0.2, 0.4, 0.6 }) Assert.Equal(Rgb.Black, Render(layer, time)[48]);
    }

    private static Rgb[] Render(IEffectLayer layer, double time, params KeyHit[] hits)
    {
        var pixels = new Rgb[83];
        var ctx = new EffectContext(TimeSpan.FromSeconds(time), TimeSpan.FromMilliseconds(16), hits, Falchion75Layout.Profile);
        layer.Render(pixels, ctx);
        return pixels;
    }
}
