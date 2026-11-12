using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class ExtendedPatternTests
{
    [Fact]
    public void ExtendedEffectsAreDistinctAndRespectBothExistingKeyboardLayouts()
    {
        foreach (var profile in new[] { Falchion75Layout.Profile, ScopeIILayout.Profile })
        {
            var signatures = new HashSet<string>();
            foreach (var kind in Enum.GetValues<PatternKind>().Where(k => k > PatternKind.Vortex))
            {
                var layer = new PatternLayer("check", kind, Rgb.White, KeyMask.All, new PatternOptions());
                var frames = new List<byte>();
                foreach (double time in new[] { .17, .63, 1.31 })
                {
                    var pixels = new Rgb[profile.LedCount];
                    layer.Render(pixels, new EffectContext(TimeSpan.FromSeconds(time), TimeSpan.Zero, [], profile));
                    foreach (var pixel in pixels) frames.AddRange([pixel.R, pixel.G, pixel.B]);
                }
                Assert.True(signatures.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(frames.ToArray()))), kind.ToString());
            }
        }
    }

    [Fact]
    public void MirroringReflectsSpatialPixelsExactly()
    {
        var profile = new KeyboardProfile("grid", 2, 3, Enumerable.Range(0,6).Select(i => new LedKey((byte)i, i.ToString(), i/3, i%3, i)).ToArray());
        Rgb[] Render(bool x, bool y)
        {
            var pixels = new Rgb[6];
            new PatternLayer("wave", PatternKind.Aurora, Rgb.White, KeyMask.All, new PatternOptions { MirrorX=x, MirrorY=y })
                .Render(pixels, new EffectContext(TimeSpan.FromSeconds(.5), TimeSpan.Zero, [], profile));
            return pixels;
        }
        var normal = Render(false,false); var mirror = Render(true,true);
        Assert.Equal(normal.Reverse(), mirror);
    }

}
