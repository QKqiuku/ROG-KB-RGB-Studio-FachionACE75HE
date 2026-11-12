using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class RevisedPatternTests
{
    private static readonly KeyboardProfile Grid = new("grid",6,15,Enumerable.Range(0,90).Select(i=>new LedKey((byte)i,i.ToString(),i/15,i%15,i)).ToArray());
    private static int Value(Rgb p)=>Math.Max(p.R,Math.Max(p.G,p.B));
    private static Rgb[] Sample(PatternKind kind,double time,PatternOptions? options=null)
    {
        var layer=new PatternLayer("test",kind,Rgb.White,KeyMask.All,options??new PatternOptions());
        var pixels=new Rgb[Grid.LedCount];
        layer.Render(pixels,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Grid));
        layer.Render(pixels,new EffectContext(TimeSpan.FromSeconds(time),TimeSpan.Zero,[],Grid));
        return pixels;
    }

    [Fact]
    public void CountdownShowsTenThenNineThroughOneBeforeFireworks()
    {
        string[] digits=["111101101101111","010110010010111","111001111100111","111001111001111","101101111001001","111100111001111","111100111101111","111001001001001","111101111101111","111101111001111"];
        for(int n=10;n>=1;n--)
        {
            var pixels=Sample(PatternKind.CountdownFireworks,10-n+.1);
            string label=n.ToString(); int start=(15-(label.Length*4-1))/2;
            for(int r=0;r<6;r++) for(int c=0;c<15;c++)
            {
                int offset=c-start, digit=offset/4, dc=offset%4;
                bool on=r<5 && offset>=0 && digit<label.Length && dc<3 && digits[label[digit]-'0'][r*3+dc]=='1';
                Assert.Equal(on,Value(pixels[r*15+c])>0);
            }
        }
    }

    [Fact]
    public void FireworksHaveSeveralDifferentOriginsAfterTheCountdown()
    {
        var centres=new List<double>();
        foreach(double time in new[]{10.6,12.0,13.4,14.8})
        {
            var pixels=Sample(PatternKind.CountdownFireworks,time);
            double sum=pixels.Sum(p=>(double)Value(p)); Assert.True(sum>100);
            centres.Add(pixels.Select((p,i)=>Value(p)*(double)(i%15)).Sum()/sum);
        }
        Assert.True(centres.Max()-centres.Min()>2);
    }

    [Theory]
    [InlineData(PatternKind.Aurora)]
    [InlineData(PatternKind.Ocean)]
    [InlineData(PatternKind.Marble)]
    public void SpeedOneHasClearlyVisibleBrightnessMotion(PatternKind kind)
    {
        var a=Sample(kind,.2);var b=Sample(kind,1.2);
        Assert.True(a.Zip(b,(x,y)=>Math.Abs(Value(x)-Value(y))).Average()>25);
    }

    [Fact]
    public void FireIsMostlyRedOrangeWithSmallYellowHotspots()
    {
        var pixels=Enumerable.Range(0,12).SelectMany(i=>Sample(PatternKind.Fire,i*.17)).ToArray();
        var lit=pixels.Where(p=>Value(p)>35).ToArray();
        double yellow=(double)lit.Count(p=>p.G>p.R*.7)/lit.Length;
        Assert.InRange(yellow,0,.2);
        Assert.Contains(lit,p=>p.R>100 && p.G<p.R*.4);
        var bottom=Enumerable.Range(0,12).SelectMany(i=>Sample(PatternKind.Fire,i*.17).Skip(75)).ToArray();
        Assert.True((double)bottom.Count(p=>p.G>p.R*.5)/bottom.Length<.35);
    }

    [Fact]
    public void RaindropsTravelDownwardWithVisibleBlueTrails()
    {
        bool falling=false;
        for(int f=0;f<20;f++)
        {
            var a=Sample(PatternKind.RainDrops,f*.1);var b=Sample(PatternKind.RainDrops,f*.1+.3);
            for(int c=0;c<15;c++) for(int r=0;r<4;r++)
                if(a[r*15+c].B>130 && b[(r+1)*15+c].B>130 && b[r*15+c].B<a[r*15+c].B-35) falling=true;
        }
        Assert.True(falling);
    }

    [Fact]
    public void OceanKeepsABroadContinuousBlueSurface()
    {
        foreach(double t in new[]{0,.3,1.2,2.4})
        {
            var pixels=Sample(PatternKind.Ocean,t);
            Assert.All(pixels,p=>Assert.True(Value(p)>=50));
            Assert.True(pixels.Average(p=>Value(p))>120);
            Assert.All(pixels,p=>Assert.True(p.B>p.R));
        }
    }

    [Theory]
    [InlineData(.25)]
    [InlineData(1)]
    [InlineData(3)]
    public void FireIncludesMovingLightInBothTopRows(double width)
    {
        var topFrames=Enumerable.Range(0,16).Select(i=>Sample(PatternKind.Fire,i*.2,new PatternOptions {Width=width}).Take(30).ToArray()).ToArray();
        Assert.All(topFrames.SelectMany(f=>f),p=>Assert.True(p.R>=3));
        for(int i=0;i<30;i++) Assert.True(topFrames.Max(f=>f[i].R)-topFrames.Min(f=>f[i].R)>15);
    }
    [Fact]
    public void FireHasDeepShadowsWithoutLosingBrightFlameTips()
    {
        var pixels=Enumerable.Range(0,16).SelectMany(i=>Sample(PatternKind.Fire,i*.2)).ToArray();
        Assert.Contains(pixels,p=>p.R<15);
        Assert.Contains(pixels,p=>p.R>170);
    }

}
