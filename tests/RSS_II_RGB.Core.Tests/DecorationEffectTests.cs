using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
namespace RSS_II_RGB.Core.Tests;
public class DecorationEffectTests
{
    [Fact]
    public void RainbowMusicKeepsMeterHeightAndBrightnessWhileColourFlows()
    {
        var state=new SensorState();var solid=new DecorationLayer("solid",state,new() {Effect=DecorationEffect.Music,Color=Rgb.White},83,12,1);
        var rainbow=new DecorationLayer("rainbow",state,new() {Effect=DecorationEffect.Music,MusicRainbow=true},83,12,1);
        var a=new Rgb[96];var b=new Rgb[96];var audio=new AudioFrame {Volume=.5};
        var ctx=new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.SpatialProfile);
        solid.Render(a,ctx,audio);rainbow.Render(b,ctx,audio);
        Assert.All(a.Zip(b),p=>Assert.InRange(Math.Abs(Math.Max(p.First.R,Math.Max(p.First.G,p.First.B))-Math.Max(p.Second.R,Math.Max(p.Second.G,p.Second.B))),0,1));
        Assert.True(b.Skip(83).Where(p=>p!=Rgb.Black).Distinct().Count()>4);
        var before=b.ToArray();rainbow.Render(b,new EffectContext(TimeSpan.FromSeconds(1),TimeSpan.Zero,[],Falchion75Layout.SpatialProfile),audio);
        Assert.False(before.SequenceEqual(b));
        rainbow.Render(b,ctx,AudioFrame.Empty);Assert.All(b,p=>Assert.Equal(Rgb.Black,p));
    }
    [Fact]
    public void CustomStripAndEyeHaveIndependentColoursWithoutChangingKeys()
    {
        var state=new SensorState();var lighting=new Falchion75Lighting();
        lighting.Configure([
            new DecorationLayer("strip",state,new() {Color=new Rgb(255,0,0)},83,12,1),
            new DecorationLayer("eye",state,new() {Color=new Rgb(0,0,255)},95,1,1)],DecorationMode.Custom,DecorationMode.Custom);
        var keys=new LedFrame(83);keys.Pixels.Fill(new Rgb(0,100,0));
        var result=lighting.Compose(keys,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile));
        Assert.All(result.Pixels[..83].ToArray(),p=>Assert.Equal(new Rgb(0,100,0),p));
        Assert.All(result.Pixels.Slice(83,12).ToArray(),p=>Assert.Equal(new Rgb(255,0,0),p));
        Assert.Equal(new Rgb(0,0,255),result[95]);
    }
    [Fact]
    public void HorizontalMusicMeterGrowsFromLeftToRightAndClearsInSilence()
    {
        var layer=new DecorationLayer("music",new SensorState(),new() {Effect=DecorationEffect.Music},83,12,1);
        var pixels=new Rgb[96];var ctx=new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.SpatialProfile);
        layer.Render(pixels,ctx,new AudioFrame {Volume=.25});
        Assert.NotEqual(Rgb.Black,pixels[83]);Assert.Equal(Rgb.Black,pixels[94]);
        layer.Render(pixels,ctx,new AudioFrame {Volume=1});Assert.NotEqual(Rgb.Black,pixels[94]);
        layer.Render(pixels,ctx,AudioFrame.Empty);Assert.All(pixels.Skip(83),p=>Assert.Equal(Rgb.Black,p));
    }
    [Theory]
    [InlineData(DecorationEffect.Rainbow)]
    [InlineData(DecorationEffect.Breathing)]
    public void IndependentAnimationDoesNotNeedAudioAndChangesWithTime(DecorationEffect effect)
    {
        var layer=new DecorationLayer("animated",new SensorState(),new() {Effect=effect},83,12,1);
        var p=new Rgb[96];var ctx=new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.SpatialProfile);
        layer.Render(p,ctx,AudioFrame.Empty);var first=p.ToArray();
        layer.Render(p,new EffectContext(TimeSpan.FromSeconds(1),TimeSpan.Zero,[],Falchion75Layout.SpatialProfile),AudioFrame.Empty);
        Assert.False(first.SequenceEqual(p));Assert.All(p.Take(83),c=>Assert.Equal(Rgb.Black,c));
    }
}
