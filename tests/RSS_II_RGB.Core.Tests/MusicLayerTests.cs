using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;

namespace RSS_II_RGB.Core.Tests;

public class MusicLayerTests
{
    public static TheoryData<MusicKind> Kinds => new(Enum.GetValues<MusicKind>());
    private static AudioFrame Signal(long beat = 1, long sequence = 1) => new()
    {
        Generation=1, Sequence=sequence, BeatId=beat,BassBeatId=beat,LowBeatId=beat, TimestampMs=1000+sequence,
        Volume=.85, Bass=.75, Mid=.65, Treble=.6, Onset=.9,
        Spectrum=Enumerable.Range(0,32).Select(i=>.35+.6*(i%7)/6).ToArray(),
        Waveform=Enumerable.Range(0,64).Select(i=>Math.Sin(i*.2)*.8).ToArray()
    };
    private static MusicLayer Layer(MusicKind kind) => new("music",kind,new SensorState(),Rgb.White,KeyMask.All);
    private static Rgb[] Render(MusicLayer layer,double time,AudioFrame frame)
    {
        var output=new Rgb[83];
        var ctx=new EffectContext(TimeSpan.FromSeconds(time),TimeSpan.FromMilliseconds(16),[],Falchion75Layout.Profile);
        layer.Render(output,ctx,frame);
        return output;
    }

    [Theory, MemberData(nameof(Kinds))]
    public void EveryKindNeedsAudioAndEventuallyClears(MusicKind kind)
    {
        var layer=Layer(kind);
        Assert.All(Render(layer,0,AudioFrame.Empty),p=>Assert.Equal(Rgb.Black,p));
        var frames=Enumerable.Range(0,10).Select(i=>Render(layer,i*.09,Signal())).ToArray();
        Assert.Contains(frames.SelectMany(p=>p),p=>p!=Rgb.Black);
        Assert.All(Render(layer,4,AudioFrame.Empty with {Generation=1}),p=>Assert.Equal(Rgb.Black,p));
        Assert.Equal(0,layer.ActivePulseCount);
        Assert.Equal(0,layer.HistoryCount);
    }

    [Fact]
    public void BeatCountersAreIndependentAndDoNotRetriggerOnRerender()
    {
        var a=Layer(MusicKind.BeatFlash);var b=Layer(MusicKind.BeatFlash);
        Assert.Equal(Render(a,0,Signal()),Render(b,0,Signal()));
        for(int i=0;i<60;i++) Render(a,i*.01,Signal());
        Assert.Equal(1,a.ActivePulseCount);
        Assert.Equal(1,b.ActivePulseCount);
        Render(a,1,Signal() with {Generation=2,BeatId=0,BassBeatId=0,LowBeatId=0});
        Assert.Equal(0,a.ActivePulseCount);
    }

    [Fact]
    public void HistoriesAreBoundedAndRewindResetsThem()
    {
        var layer=Layer(MusicKind.SpectrumWaterfall);
        for(int i=1;i<=1000;i++) Render(layer,i*.001,Signal(i,i));
        Assert.InRange(layer.HistoryCount,1,64);
        Assert.InRange(layer.ActivePulseCount,1,32);
        Render(layer,0,Signal(0,0));
        Assert.Equal(1,layer.HistoryCount);
        Assert.Equal(0,layer.ActivePulseCount);
    }

    [Theory]
    [InlineData(MusicKind.SpectrumBars)]
    [InlineData(MusicKind.NeonSpectrum)]
    [InlineData(MusicKind.SoftSpectrum)]
    [InlineData(MusicKind.RadialSpectrum)]
    public void SpectrumGeometryUsesFrequencyDistribution(MusicKind kind)
    {
        var bass=Signal() with {Spectrum=Enumerable.Range(0,32).Select(i=>i<10?.9:.05).ToArray()};
        var treble=bass with {Spectrum=bass.Spectrum.Reverse().ToArray()};
        Assert.False(Render(Layer(kind),0,bass).SequenceEqual(Render(Layer(kind),0,treble)));
    }

    [Fact]
    public void WaveformsUseActualSamplesAndBreathingUsesVolume()
    {
        var f=Signal();
        Assert.False(Render(Layer(MusicKind.WaveformBounce),0,f).SequenceEqual(
            Render(Layer(MusicKind.WaveformBounce),0,f with {Waveform=new double[64]})));
        Assert.False(Render(Layer(MusicKind.VolumeBreathing),0,f).SequenceEqual(
            Render(Layer(MusicKind.VolumeBreathing),0,f with {Volume=.1})));
    }

    [Theory, MemberData(nameof(Kinds))]
    public void DecorativeCoordinatesUseIndependentBeatHistory(MusicKind kind)
    {
        var layer=Layer(kind);
        var output=new Rgb[96];
        var ctx=new EffectContext(TimeSpan.FromSeconds(.2),TimeSpan.Zero,[],Falchion75Layout.SpatialProfile);
        layer.Render(output,ctx,Signal());
        Assert.Equal(1,layer.ActivePulseCount);
        Assert.Contains(output,p=>p!=Rgb.Black);
        layer.Render(output,ctx,Signal());
        Assert.Equal(1,layer.ActivePulseCount);
    }

    [Fact]
    public void SpectralFamiliesHaveDifferentSpatialShapes()
    {
        var kinds=new[]{MusicKind.SpectrumBars,MusicKind.SoftSpectrum,
            MusicKind.SpectrumRing,MusicKind.BidirectionalSpectrum,MusicKind.RadialSpectrum};
        var frames=kinds.Select(k=>Render(new MusicLayer("shape",k,new SensorState(),Rgb.White,
            KeyMask.All,new PatternOptions{Rainbow=false}),0,Signal())).ToArray();
        for(int a=0;a<frames.Length;a++)
            for(int b=a+1;b<frames.Length;b++) Assert.False(frames[a].SequenceEqual(frames[b]));
    }

    [Theory, MemberData(nameof(Kinds))]
    public void NonFiniteInputCannotCreateLightWithoutEnergy(MusicKind kind)
    {
        var malformed=Signal() with {Volume=double.NaN,Onset=double.PositiveInfinity,
            Spectrum=[double.NaN,double.PositiveInfinity],Waveform=[double.NaN,double.NegativeInfinity]};
        Assert.All(Render(Layer(kind),0,malformed),p=>Assert.Equal(Rgb.Black,p));
    }

    [Fact]
    public void EqualizerRunsGreenToRedUpwardAndHasWhiteCaps()
    {
        var pixels=VerticalPalette(MusicKind.ClassicEqualizer);
        Assert.True(pixels[10].G>pixels[10].R);
        Assert.True(pixels[3].R>pixels[3].G);
        Assert.Contains(pixels.Take(2),p=>p==Rgb.White);
        Assert.True(pixels[2].R>pixels[2].G);
    }

    [Fact]
    public void FireHasSaturatedYellowBaseAndOrangeUpperFlames()
    {
        var pixels=VerticalPalette(MusicKind.RhythmFire);
        Assert.True(pixels[10].G>pixels[5].G);
        Assert.Equal(0,pixels[10].B);
        Assert.True(pixels[5].R>pixels[5].G);
    }

    private static Rgb[] VerticalPalette(MusicKind kind)
    {
        var layout=new KeyboardProfile("palette",11,1,Enumerable.Range(0,11)
            .Select(i=>new LedKey((byte)i,i.ToString(),i,0,i)).ToArray());
        var pixels=new Rgb[11];
        var ctx=new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],layout);
        Layer(kind).Render(pixels,ctx,Signal() with {Spectrum=[.5],Bass=.8});
        return pixels;
    }

    [Fact]
    public void StandardRenderRejectsStaleSnapshots()
    {
        var layer=Layer(MusicKind.BeatFlash);
        var pixels=new Rgb[83];
        var ctx=new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile);
        layer.Render(pixels,ctx);
        Assert.All(pixels,p=>Assert.Equal(Rgb.Black,p));
    }
}
