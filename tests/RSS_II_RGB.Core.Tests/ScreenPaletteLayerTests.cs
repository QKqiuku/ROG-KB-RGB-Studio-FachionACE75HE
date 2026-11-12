using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class ScreenPaletteLayerTests
{
    private sealed class Source : IScreenColorSource
    {
        public int Width => 16;
        public int Height => 9;
        public ReadOnlyMemory<Rgb> Colors { get; set; }
    }

    [Fact]
    public void EmptyIsBlackAndSceneColorsDriveTheWash()
    {
        var source = new Source();
        var layer = Layer(source);
        Assert.All(Render(layer), p => Assert.Equal(Rgb.Black, p));
        source.Colors = Enumerable.Repeat(new Rgb(255, 0, 0), 144).ToArray();
        Assert.All(Render(layer), p => Assert.True(p.R > p.B && p.R > p.G && p.G > 0));
        source.Colors = Enumerable.Repeat(new Rgb(0, 0, 255), 144).ToArray();
        Assert.All(Render(layer, 10), p => Assert.True(p.B > p.R));
    }

    [Fact]
    public void MirrorsAndDirectionTransformSceneCoordinates()
    {
        var source = Gradient();
        var normal = Render(Layer(source));
        var mirror = Render(Layer(source, new PatternOptions { MirrorX = true }));
        var rotated = Render(Layer(source, new PatternOptions { Direction = 180 }));
        Assert.Equal(normal[0], mirror[^1]);
        Assert.Equal(mirror, rotated);
    }

    [Fact]
    public void WidthSaturationAndSingleColorAffectTheSample()
    {
        var source = Gradient();
        Assert.NotEqual(Render(Layer(source, new PatternOptions { Width = 0.25 }))[0],
            Render(Layer(source, new PatternOptions { Width = 3 }))[0]);
        Assert.All(Render(Layer(source, new PatternOptions { Saturation = 0 })), p => Assert.True(p.R == p.G && p.G == p.B));
        var mono = new ScreenPaletteLayer("mono", source, KeyMask.All, new PatternOptions { Rainbow = false }, color: new Rgb(0, 255, 0));
        Assert.All(Render(mono), p => Assert.True(p.R == 0 && p.B == 0 && p.G > 0));
    }

    [Fact]
    public void SpeedControlsResponseToNewScene()
    {
        var source = Gradient();
        var slow = Layer(source, new PatternOptions { Speed = 0.1 });
        var fast = Layer(source, new PatternOptions { Speed = 5 });
        Render(slow); Render(fast);
        source.Colors = Enumerable.Repeat(Rgb.White, 144).ToArray();
        Assert.True(Render(fast)[0].B > Render(slow)[0].B);
    }

    private static Source Gradient() => new() { Colors = Enumerable.Range(0, 144)
        .Select(i => new Rgb((byte)(i % 16 * 17), 0, (byte)(255 - i % 16 * 17))).ToArray() };
    private static ScreenPaletteLayer Layer(Source source, PatternOptions? options = null)
        => new("screen", source, KeyMask.All, options ?? new PatternOptions());
    private static Rgb[] Render(IEffectLayer layer, double delta = 0.1)
    {
        var layout = new KeyboardProfile("test", 1, 2, [new(1, "left", 0, 0, 0), new(2, "right", 0, 1, 1)]);
        var result = new Rgb[2];
        layer.Render(result, new EffectContext(TimeSpan.Zero, TimeSpan.FromSeconds(delta), [], layout));
        return result;
    }
}
