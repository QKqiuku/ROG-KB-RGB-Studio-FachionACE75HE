using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
namespace RSS_II_RGB.Core.Tests;
public class MusicSpectrumExtentTests
{
    [Theory]
    [InlineData(MusicKind.SpectrumBars)]
    [InlineData(MusicKind.ClassicEqualizer)]
    [InlineData(MusicKind.MirrorSpectrum)]
    [InlineData(MusicKind.NeonSpectrum)]
    [InlineData(MusicKind.BidirectionalSpectrum)]
    [InlineData(MusicKind.RhythmFire)]
    public void StrongSpectrumReachesEveryPhysicalLightBarPosition(MusicKind kind)
    {
        Assert.Equal(-.2,MusicLayer.CanvasBounds(kind).Top);
        var layer=new MusicLayer("extent",kind,new SensorState(),Rgb.White,KeyMask.All);
        var profile=Falchion75Layout.SpatialProfile;
        var pixels=new Rgb[profile.LedCount];
        var frame=new AudioFrame {Generation=1,Volume=1,Bass=1,Spectrum=Enumerable.Repeat(1.0,32).ToArray()};
        layer.Render(pixels,new EffectContext(TimeSpan.Zero,TimeSpan.FromMilliseconds(16),[],profile),frame);
        Assert.All(pixels.Skip(83),p=>Assert.True(Math.Max(p.R,Math.Max(p.G,p.B))>60));
    }
}
