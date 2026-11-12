using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class LumoAmbientTests
{
    [Theory]
    [InlineData(PatternKind.Caustics, .45, .8)]
    [InlineData(PatternKind.Sun, .40, .75)]
    [InlineData(PatternKind.IceCrystal, .65, .90)]
    [InlineData(PatternKind.DoubleBeacon, .78, 1.0)]
    public void BrightFieldsRetainTheirReferenceBrightness(PatternKind kind, double minimum, double maximum)
    {
        foreach (var profile in new[] { Falchion75Layout.Profile, ScopeIILayout.Profile })
        foreach (double t in new[] { 0.0, 1.5, 4.0 })
            Assert.InRange(Frame(kind, t, profile: profile).Average(p => Math.Max(p.R, Math.Max(p.G, p.B)) / 255.0), minimum, maximum);
    }

    public static TheoryData<PatternKind> Patterns => new(
        new[] { PatternKind.SineWave }.Concat(Enum.GetValues<PatternKind>().Where(k => k >= PatternKind.Marquee)));

    [Theory]
    [MemberData(nameof(Patterns))]
    public void BothLayoutsAnimateInColorAndMonochrome(PatternKind kind)
    {
        foreach (var profile in new[] { Falchion75Layout.Profile, ScopeIILayout.Profile })
        foreach (bool rainbow in new[] { true, false })
        {
            var options = new PatternOptions { Rainbow = rainbow };
            var first = Frame(kind, .17, options, profile);
            Assert.Contains(first, p => p.R + p.G + p.B > 0);
            Assert.Contains(new[] { .8, 2.1, 3.5 }, t => !first.SequenceEqual(Frame(kind, t, options, profile)));
            if (!rainbow) Assert.All(first, p => { Assert.Equal(0, p.G); Assert.Equal(0, p.B); });
        }
    }

    [Theory]
    [MemberData(nameof(Patterns))]
    public void AdvertisedWidthAndTransformsAffectGeometry(PatternKind kind)
    {
        var first = Frame(kind, .8);
        Assert.False(first.SequenceEqual(Frame(kind, .8, new PatternOptions { Width = 2.3 })));
        Assert.False(first.SequenceEqual(Frame(kind, .8, new PatternOptions { Direction = 73, MirrorX = true })));
        Assert.Equal(Frame(kind, 1.6), Frame(kind, .8, new PatternOptions { Speed = 2 }));
    }

    [Theory]
    [InlineData(PatternKind.Sakura)]
    [InlineData(PatternKind.IceCrystal)]
    public void ParticleDensityChangesTheFrame(PatternKind kind) =>
        Assert.False(Frame(kind, 1, new PatternOptions { Density = .05 }).SequenceEqual(
            Frame(kind, 1, new PatternOptions { Density = 1 })));

    [Fact]
    public void LakeAndAngularFamiliesHaveDistinctFields()
    {
        PatternKind[] kinds = [PatternKind.LakePixels, PatternKind.LakeRipples, PatternKind.LakeSurge,
            PatternKind.RotatingRing, PatternKind.WindmillCycle,
            PatternKind.SineWave, PatternKind.SineSectors];
        for (int i = 0; i < kinds.Length; i++)
        for (int j = i + 1; j < kinds.Length; j++)
            Assert.False(Frame(kinds[i], .7).SequenceEqual(Frame(kinds[j], .7)));
    }

    [Fact]
    public void CurveTracesASinusoidWhileSectorsKeepDimAndBrightRegions()
    {
        var profile = new KeyboardProfile("grid", 31, 41,
            Enumerable.Range(0, 1271).Select(i => new LedKey(0, "", i / 41, i % 41, i)).ToArray());
        var curve = Frame(PatternKind.SineWave, 0, profile: profile);
        for (int col = 0; col < 41; col++)
        {
            int brightestRow = Enumerable.Range(0, 31).MaxBy(row =>
            { var p = curve[row * 41 + col]; return Math.Max(p.R, Math.Max(p.G, p.B)); });
            double expected = (.5 + .32 * Math.Sin(col / 40.0 * Math.PI * 2)) * 30;
            Assert.InRange(Math.Abs(brightestRow - expected), 0, 1.1);
        }
        var sectors = Frame(PatternKind.SineSectors, 0, profile: profile);
        Assert.DoesNotContain(sectors, p => p == Rgb.Black);
        Assert.Contains(sectors, p => Math.Max(p.R,Math.Max(p.G,p.B)) <= 17);
        Assert.Contains(sectors, p => Math.Max(p.R,Math.Max(p.G,p.B)) > 240);
    }

    [Fact]
    public void PerimeterFlowFillsTheInterior()
    {
        Assert.All(Frame(PatternKind.PerimeterFlow, 0), p => Assert.True(Math.Max(p.R, Math.Max(p.G, p.B)) > 240));
    }

    private static Rgb[] Frame(PatternKind kind, double time, PatternOptions? options = null, KeyboardProfile? profile = null)
    {
        profile ??= Falchion75Layout.Profile;
        var pixels = new Rgb[profile.LedCount];
        var ctx = new EffectContext(TimeSpan.FromSeconds(time), TimeSpan.FromMilliseconds(16), [], profile);
        new PatternLayer("test", kind, new Rgb(255, 0, 0), KeyMask.All, options ?? new()).Render(pixels, ctx);
        return pixels;
    }
}
