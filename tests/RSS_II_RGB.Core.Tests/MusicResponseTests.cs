using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
namespace RSS_II_RGB.Core.Tests;
public class MusicResponseTests
{
    private static AudioFrame NormalMusic()
    {
        var analyzer=new AudioAnalyzer();
        for(int hop=0;hop<200;hop++)
        {
            var pcm=new float[1024];
            for(int i=0;i<512;i++)
            {
                double t=(hop*512+i)/48000.0;
                float v=(float)(.06*Math.Sin(2*Math.PI*110*t)+.035*Math.Sin(2*Math.PI*440*t)+.025*Math.Sin(2*Math.PI*2200*t));
                pcm[i*2]=pcm[i*2+1]=v;
            }
            analyzer.Push(pcm,2,48000,1000+hop*512L*1000/48000);
        }
        return analyzer.Latest;
    }
    private static Rgb[] Draw(MusicKind kind,AudioFrame signal)
    {
        var layer=new MusicLayer("test",kind,new SensorState(),Rgb.White,KeyMask.All);
        var pixels=new Rgb[83];
        layer.Render(pixels,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile),signal);
        return pixels;
    }
    [Fact]
    public void NormalPcmProducesVisibleBrightnessWithoutIncreasingSensitivity()
    {
        var pixels=Draw(MusicKind.VolumeBreathing,NormalMusic());
        Assert.True(pixels.Max(p=>Math.Max(p.R,Math.Max(p.G,p.B)))>=100);
    }
    [Theory]
    [InlineData(MusicKind.BottomEnergyCore)]
    public void NormalPcmCoreReachesAcrossTheKeyboard(MusicKind kind)
    {
        var pixels=Draw(kind,NormalMusic());
        var lit=Falchion75Layout.Profile.Keys.ToArray().Where(k=>Math.Max(pixels[k.Index].R,Math.Max(pixels[k.Index].G,pixels[k.Index].B))>=16).ToArray();
        Assert.True(lit.Length>=40,$"Only {lit.Length} visible keys");
        Assert.Contains(lit,k=>k.Col<=3);Assert.Contains(lit,k=>k.Col>=14);
    }
    [Fact]
    public void QuietWaveformStillUsesTheBoardHeightWhileVolumeRemainsAudible()
    {
        var frame=NormalMusic() with {Waveform=Enumerable.Range(0,128).Select(i=>.05*Math.Sin(i*.15)).ToArray()};
        var pixels=Draw(MusicKind.SoundWave,frame);
        var rows=Falchion75Layout.Profile.Keys.ToArray().Where(k=>Math.Max(pixels[k.Index].R,Math.Max(pixels[k.Index].G,pixels[k.Index].B))>=24).Select(k=>k.Row).Distinct().ToArray();
        Assert.True(rows.Length>=3,$"Only {rows.Length} waveform rows");
    }
    [Fact]
    public void DisplayLiftPreservesSilenceAndLoudnessOrdering()
    {
        var normal=NormalMusic();
        int Peak(AudioFrame f)=>Draw(MusicKind.VolumeBreathing,f).Max(p=>Math.Max(p.R,Math.Max(p.G,p.B)));
        Assert.Equal(0,Peak(normal with {Volume=0}));
        Assert.True(Peak(normal with {Volume=.04})<Peak(normal with {Volume=.12}));
        Assert.True(Peak(normal with {Volume=.12})<Peak(normal with {Volume=.6}));
    }
}
