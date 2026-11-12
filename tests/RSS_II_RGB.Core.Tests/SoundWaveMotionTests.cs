using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
namespace RSS_II_RGB.Core.Tests;
public class SoundWaveMotionTests
{
    private static Rgb[] Draw(double time,double volume=.3)
    {
        var layer=new MusicLayer("soundwave",MusicKind.SoundWave,new SensorState(),Rgb.White,KeyMask.All,new PatternOptions {Rainbow=false,WaveColor1=new Rgb(0,255,255),WaveColor2=new Rgb(255,0,255)});
        var pixels=new Rgb[401*201];layer.RenderCanvas(pixels,401,201,TimeSpan.FromSeconds(time),new AudioFrame {Generation=1,Sequence=1,Volume=volume,Bass=volume},TestContext.Current.CancellationToken,-.2,1.2);
        return pixels;
    }
    private static int Center(Rgb[] image,int x,bool upper)=>Enumerable.Range(0,201).MaxBy(y=>upper?image[y*401+x].G:image[y*401+x].R);
    [Fact]
    public void WaveOnlyUsesBassAndIgnoresLouderMidsAndTreble()
    {
        var layer=new MusicLayer("bass",MusicKind.SoundWave,new SensorState(),Rgb.White,KeyMask.All);
        Rgb[] Sample(double bass,double volume,double treble)
        {
            var p=new Rgb[401*201];
            layer.RenderCanvas(p,401,201,TimeSpan.Zero,new AudioFrame {Generation=1,Volume=volume,Bass=bass,Mid=treble,Treble=treble},TestContext.Current.CancellationToken,-.2,1.2);
            return p;
        }
        Assert.Equal(Sample(.06,.1,0),Sample(.06,1,1));
        Assert.All(Sample(0,1,1),p=>Assert.Equal(Rgb.Black,p));
        Assert.False(Sample(.02,.3,0).SequenceEqual(Sample(.2,.3,0)));
    }
    [Fact]
    public void CustomWavesKeepTheirIndependentColoursAndRainbowFlowsAcrossThem()
    {
        var custom=new MusicLayer("colours",MusicKind.SoundWave,new SensorState(),Rgb.White,KeyMask.All,
            new PatternOptions {Rainbow=false,WaveColor1=new Rgb(255,0,0),WaveColor2=new Rgb(0,0,255)});
        var pixels=new Rgb[401*201];var f=new AudioFrame {Generation=1,Volume=.3,Bass=.3};
        custom.RenderCanvas(pixels,401,201,TimeSpan.Zero,f,TestContext.Current.CancellationToken,-.2,1.2);
        Assert.All(pixels,p=>Assert.Equal(0,p.G));
        Assert.Contains(pixels,p=>p.R>50 && p.B==0);Assert.Contains(pixels,p=>p.B>50 && p.R==0);
        var rainbow=new MusicLayer("rainbow",MusicKind.SoundWave,new SensorState(),Rgb.White,KeyMask.All);
        rainbow.RenderCanvas(pixels,401,201,TimeSpan.Zero,f,TestContext.Current.CancellationToken,-.2,1.2);
        Assert.Contains(pixels,p=>p.R>50 && p.G<10 && p.B<10);
        Assert.Contains(pixels,p=>p.G>50 && p.R<10 && p.B<10);
        var first=pixels.ToArray();
        rainbow.RenderCanvas(pixels,401,201,TimeSpan.FromSeconds(1),f,TestContext.Current.CancellationToken,-.2,1.2);
        Assert.False(first.SequenceEqual(pixels));
    }
    [Fact]
    public void BeamsHaveDifferentRightwardSpeeds()
    {
        var before=Draw(0);var after=Draw(1);
        for(int y=0;y<201;y++)
        {
            Assert.InRange(Math.Abs(before[y*401+100].G-after[y*401+116].G),0,1);
            Assert.InRange(Math.Abs(before[y*401+100].R-after[y*401+124].R),0,1);
        }
    }
    [Fact]
    public void AudioMovesWavePointsAcrossTheFullAllowedRegion()
    {
        var quiet=Draw(0,.0001);var loud=Draw(0,.3);
        var a=Enumerable.Range(0,401).Select(x=>Center(quiet,x,true)).ToArray();
        var b=Enumerable.Range(0,401).Select(x=>Center(loud,x,true)).ToArray();
        Assert.True(b.Max()-b.Min()>a.Max()-a.Min()+30);
        Assert.InRange(b.Min(),0,2);
        Assert.InRange(b.Max(),198,200);
        Assert.InRange(Math.Abs(b.Average()-a.Average()),0,2);
    }
    [Fact]
    public void BothWavesCrossTheSameCentreAndTheirCrestsSpanQwertyToBar()
    {
        var image=Draw(0);
        var upper=Enumerable.Range(0,401).Select(x=>Center(image,x,true)).ToArray();
        var lower=Enumerable.Range(0,401).Select(x=>Center(image,x,false)).ToArray();
        Assert.All(new[]{upper,lower},wave=>
        {
            Assert.InRange(wave.Average(),98,102);
            Assert.InRange(wave.Min(),0,2);
            Assert.InRange(wave.Max(),198,200);
        });
        var quiet=Draw(0,.0001);
        foreach(bool cyan in new[]{true,false})
        {
            var wave=Enumerable.Range(0,401).Select(x=>Center(quiet,x,cyan)).ToArray();
            Assert.InRange(wave.Min(),70,86);
            Assert.InRange(wave.Max(),112,130);
        }
        Assert.Contains(Enumerable.Range(0,401),x=>Center(image,x,true)<Center(image,x,false));
        Assert.Contains(Enumerable.Range(0,401),x=>Center(image,x,true)>Center(image,x,false));
        Assert.All(Draw(.8,0),p=>Assert.Equal(Rgb.Black,p));
    }
}
