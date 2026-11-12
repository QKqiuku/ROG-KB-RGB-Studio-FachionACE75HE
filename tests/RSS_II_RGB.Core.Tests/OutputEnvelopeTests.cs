using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
namespace RSS_II_RGB.Core.Tests;
public class OutputEnvelopeTests
{
    [Fact]
    public void ConstantOpacityAttenuatesEveryKeyStripAndEyeIncludingZero()
    {
        foreach(double alpha in new[]{0,.5,1})
        {
            var pixels=Enumerable.Repeat(new Rgb(200,100,50),96).ToArray();
            new OutputEnvelope(new SensorState(),new() {Opacity=alpha}).Apply(pixels,TimeSpan.Zero,AudioFrame.Empty);
            Assert.All(pixels,p=>Assert.Equal(new Rgb(200,100,50).Scale((float)alpha),p));
        }
    }
    [Fact]
    public void BreathingUsesElapsedTimeAndStaysWithinItsOpacityBounds()
    {
        var effect=new OutputEnvelope(new SensorState(),new() {Mode=OutputEnvelopeMode.Breathing,Opacity=.8,Minimum=.2});
        byte Sample(double time) {var p=new[]{Rgb.White};effect.Apply(p,TimeSpan.FromSeconds(time),AudioFrame.Empty);return p[0].R;}
        Assert.Equal(204,Sample(0));Assert.Equal(51,Sample(1));Assert.Equal(204,Sample(2));
    }
    [Fact]
    public void BassFlashesDecayWithoutRetriggeringAndResetWithTheSource()
    {
        var effect=new OutputEnvelope(new SensorState(),new() {Mode=OutputEnvelopeMode.BeatFlash,Minimum=0});
        byte Sample(double t,AudioFrame f) {var p=new[]{Rgb.White};effect.Apply(p,TimeSpan.FromSeconds(t),f);return p[0].R;}
        var high=new AudioFrame {Generation=1,BeatId=1,Treble=1};
        Assert.Equal(0,Sample(0,high));
        var low=high with {BassBeatId=1,Bass=.3};
        Assert.Equal(255,Sample(.1,low));Assert.InRange(Sample(.4,low),1,80);
        Assert.Equal(0,Sample(.5,AudioFrame.Empty with {Generation=2}));
    }
    [Fact]
    public void MalformedOptionsCannotAmplifyOrCorruptOutput()
    {
        var p=Enumerable.Repeat(Rgb.White,96).ToArray();
        new OutputEnvelope(new SensorState(),new() {Opacity=double.NaN,Minimum=9,Speed=double.PositiveInfinity,Mode=(OutputEnvelopeMode)999}).Apply(p,TimeSpan.Zero,AudioFrame.Empty);
        Assert.All(p,c=>Assert.Equal(Rgb.White,c));
    }
}
