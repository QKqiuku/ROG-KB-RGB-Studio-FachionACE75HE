using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Layout;

namespace RSS_II_RGB.Core.Tests;

public class EqualizerPeakTests
{
    private static MusicLayer Layer() => new("caps", MusicKind.ClassicEqualizer,
        new SensorState(), new Rgb(200, 30, 10), KeyMask.All, new PatternOptions { Rainbow = false });
    private static AudioFrame Signal(double amplitude, long generation = 1) => new()
    {
        Generation = generation, Volume = amplitude > 0 ? .5 : 0,
        Spectrum = Enumerable.Repeat(amplitude, 32).ToArray()
    };
    private static Rgb[] Render(MusicLayer layer, double time, AudioFrame frame, int height = 201)
    {
        var pixels = new Rgb[641 * height];
        layer.RenderCanvas(pixels, 641, height, TimeSpan.FromSeconds(time), frame,TestContext.Current.CancellationToken);
        return pixels;
    }
    private static double CapHeight(Rgb[] pixels)
    {
        int[] rows = Enumerable.Range(0, pixels.Length / 641)
            .Where(row => pixels[row * 641+10] == Rgb.White).ToArray();
        Assert.NotEmpty(rows);
        return 1 - rows.Average() / (pixels.Length / 641 - 1);
    }

    [Fact]
    public void AirborneCapIsSampledAtActualDecorativeLedPositions()
    {
        var layer=Layer();var pixels=new Rgb[Falchion75Layout.SpatialProfile.LedCount];
        void Draw(double t,double amplitude)
        {
            var ctx=new EffectContext(TimeSpan.FromSeconds(t),TimeSpan.FromMilliseconds(16),[],Falchion75Layout.SpatialProfile);
            layer.Render(pixels,ctx,Signal(amplitude));
        }
        Draw(0,.1);Draw(.1,.8);Draw(.3,.8);
        Assert.Contains(pixels.Skip(83),p=>p==Rgb.White);
    }
    [Fact]
    public void RisingCapCanReachTheLightBarAboveTheFunctionRow()
    {
        var layer=Layer();
        var pixels=new Rgb[641*241];
        void Draw(double t,double amplitude)=>layer.RenderCanvas(pixels,641,241,TimeSpan.FromSeconds(t),Signal(amplitude),TestContext.Current.CancellationToken,-.2,1);
        Draw(0,.1);Draw(.1,.8);Draw(.3,.8);
        Assert.Contains(Enumerable.Range(0,40).SelectMany(y=>pixels.Skip(y*641).Take(641)),p=>p==Rgb.White);
    }
    [Fact]
    public void RisingColumnThrowsWhiteCapAboveBarThenLandsWithoutRepeatedBounces()
    {
        var layer = Layer();
        Render(layer, 0, Signal(.02));
        double bar = CapHeight(Render(layer, .1, Signal(.08)));
        double airborne = CapHeight(Render(layer, .25, Signal(.08)));
        Assert.True(airborne > bar + .04);
        double landed = CapHeight(Render(layer, 2, Signal(.08)));
        Assert.InRange(Math.Abs(landed - bar), 0, .006);
        Assert.Equal(Render(layer, 2, Signal(.08)), Render(layer, 3, Signal(.08)));
    }

    [Fact]
    public void SilenceAllowsExistingCapToFallAndEventuallyClears()
    {
        var layer = Layer();
        Render(layer, 0, Signal(.02));
        Render(layer, .1, Signal(.08));
        Assert.Contains(Render(layer, .2, Signal(0)), p => p == Rgb.White);
        Assert.All(Render(layer, 3, Signal(0)), p => Assert.Equal(Rgb.Black, p));
        Assert.All(Render(Layer(), 0, Signal(0)), p => Assert.Equal(Rgb.Black, p));
    }

    [Fact]
    public void BallisticFlightUsesElapsedTimeAcrossRenderRates()
    {
        static double Flight(int fps)
        {
            var layer = Layer();
            Render(layer, 0, Signal(.02));
            Render(layer, .2, Signal(.08));
            for (int i = 1; i < fps / 5; i++) Render(layer, .2 + i / (double)fps, Signal(.08));
            return CapHeight(Render(layer, .4, Signal(.08)));
        }
        Assert.InRange(Math.Abs(Flight(15) - Flight(60)), 0, .006);
    }

    [Fact]
    public void SourceChangeAndClockRewindDiscardAirborneCaps()
    {
        foreach (bool rewind in new[] { false, true })
        {
            var layer = Layer();
            Render(layer, 0, Signal(.02));
            Render(layer, .1, Signal(.08));
            Render(layer, .2, Signal(.08));
            Assert.All(Render(layer, rewind ? 0 : .3, Signal(0, rewind ? 1 : 2)),
                p => Assert.Equal(Rgb.Black, p));
        }
    }

    [Fact]
    public void ShortCanvasKeepsCapsVisibleAndZeroBinsStayDark()
    {
        var layer = Layer();
        var frame = Signal(.08) with { Spectrum = [0, .08, 0, .02] };
        var pixels = Render(layer, 0, frame, 6);
        Assert.Contains(pixels, p => p == Rgb.White);
        Assert.All(Enumerable.Range(0, 6).Select(row => pixels[row * 641]), p => Assert.Equal(Rgb.Black, p));
    }
}
