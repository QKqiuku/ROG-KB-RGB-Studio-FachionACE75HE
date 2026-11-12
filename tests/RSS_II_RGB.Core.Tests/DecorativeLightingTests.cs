using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Animation;
using System.Text.Json;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;
namespace RSS_II_RGB.Core.Tests;
public class DecorativeLightingTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public void BarAndEyeModesAreIndependentAndNeverChangeKeyPixels(bool bar, bool eye)
    {
        var lighting = new Falchion75Lighting();
        lighting.Configure([new PositionLayer()], bar ? DecorationMode.Spatial : DecorationMode.FollowKeys,
            eye ? DecorationMode.Spatial : DecorationMode.FollowKeys);
        var keys = new LedFrame(83);
        for(int i=0;i<83;i++)keys[i]=new Rgb((byte)i,20,40);
        var output=lighting.Compose(keys,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile));
        Assert.Equal(96,output.Length);
        Assert.Equal(keys.Pixels.ToArray(),output.Pixels[..83].ToArray());
        for(int i=0;i<13;i++)Assert.Equal((i==12?eye:bar) ? new Rgb(0,0,255) : keys[Falchion75Layout.LightBarSourceIndices[i]],output[83+i]);
    }
    [Fact]
    public void AllProceduralPresetsAcceptPositionsAboveTheKeyboard()
    {
        foreach(var kind in Enum.GetValues<PatternKind>())
        {
            var layer = new PatternLayer("pattern",kind,new Rgb(120,80,200),KeyMask.All,new PatternOptions());
            var frame = new LedFrame(96);
            foreach(double time in new[] { 1.0, 12.0 })
            {
                var ctx = new EffectContext(TimeSpan.FromSeconds(time),TimeSpan.FromMilliseconds(16),[new KeyHit(46,TimeSpan.FromSeconds(time-.1))],Falchion75Layout.SpatialProfile);
                try { layer.Render(frame.Pixels,ctx); }
                catch(Exception error) { Assert.Fail($"{kind} failed above the keyboard: {error}"); }
            }
        }
    }

    [Fact]
    public void AllKeyInteractionEffectsAcceptDecorativePositions()
    {
        foreach(var kind in Enum.GetValues<KeyEffectKind>())
        {
            var layer=new ReactivePatternLayer("interaction",kind,new Rgb(120,80,200),KeyMask.All,new PatternOptions());
            var frame=new LedFrame(96);
            foreach(double time in new[] { 1.0, 1.3 })
            {
                var ctx=new EffectContext(TimeSpan.FromSeconds(time),TimeSpan.FromMilliseconds(16),[new KeyHit(0,TimeSpan.FromSeconds(1))],Falchion75Layout.SpatialProfile);
                layer.Render(frame.Pixels,ctx);
            }
        }
    }

    [Fact]
    public void AuthoredAnimationUsesItsOwnTopEdgeBeforeKeyZoneOverrides()
    {
        using var json = JsonDocument.Parse("""{"version":1,"effects":[{"type":"keyframe","config":{"customParams":{"sourceRows":6,"sourceCols":15,"frames":[{"0_0":"#0000FF","0_14":"#00FF00"}]}}}]}""");
        var animation = AtkScene.Parse(json.RootElement,Falchion75Layout.Profile,false);
        var lighting = new Falchion75Lighting();
        lighting.Configure([new AtkSceneLayer("animation",animation,KeyMask.All)],DecorationMode.Spatial,DecorationMode.Spatial);
        var keys = new LedFrame(83);keys[0]=new Rgb(255,0,0);
        var frame = lighting.Compose(keys,new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile));
        Assert.Equal(new Rgb(255,0,0),frame[0]);
        Assert.Equal(new Rgb(0,0,255),frame[83]);
        Assert.Equal(new Rgb(0,255,0),frame[95]);
        Assert.Equal(Rgb.Black,frame[84]);
    }

    [Fact]
    public void FittedAtkUsesRawTopEdgeEvenWhenTopKeysSampleSecondSourceRow()
    {
        using var json=JsonDocument.Parse("""{"version":1,"effects":[{"type":"keyframe","config":{"customParams":{"sourceRows":12,"sourceCols":15,"frames":[{"0_0":"#0000FF","1_0":"#FF0000"}]}}}]}""");
        var scene=AtkScene.Parse(json.RootElement,Falchion75Layout.Profile,true);
        var normal=new Rgb[83];scene.Render(normal,TimeSpan.Zero);
        Assert.Equal(new Rgb(255,0,0),normal[0]);
        var lighting=new Falchion75Lighting();
        lighting.Configure([new AtkSceneLayer("fitted",scene,KeyMask.All)],DecorationMode.Spatial,DecorationMode.Spatial);
        var frame=lighting.Compose(new LedFrame(83),new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile));
        Assert.Equal(new Rgb(0,0,255),frame[83]);
    }

    [Fact]
    public void GifSamplesTopEdgeForDecorativePositions()
    {
        var pixels = new Rgb[15*6];pixels[0]=new Rgb(255,0,0);pixels[14]=new Rgb(0,255,0);
        var animation = new KbAnim(15,6,[new KbAnim.Frame(100,pixels)]);
        var lighting = new Falchion75Lighting();
        lighting.Configure([new GifLayer("gif",animation,KeyMask.All)],DecorationMode.Spatial,DecorationMode.Spatial);
        var frame=lighting.Compose(new LedFrame(83),new EffectContext(TimeSpan.Zero,TimeSpan.Zero,[],Falchion75Layout.Profile));
        Assert.Equal(new Rgb(255,0,0),frame[83]);
        Assert.Equal(new Rgb(0,255,0),frame[95]);
    }

    [Fact]
    public void CustomModeIsAvailableAndInvalidModesFallBackToFollow()
    {
        Assert.Equal(DecorationMode.Custom,Falchion75Lighting.Normalize(DecorationMode.Custom));
        Assert.Equal(DecorationMode.FollowKeys,Falchion75Lighting.Normalize((DecorationMode)999));
    }

    private sealed class PositionLayer : IEffectLayer
    {
        public string Id=>"position"; public int ZOrder=>0; public BlendMode Blend=>BlendMode.Normal;
        public KeyMask Mask=>KeyMask.All; public bool IsComplete=>false;
        public void Render(Span<Rgb> target,in EffectContext ctx)
        {for(int i=0;i<target.Length;i++)target[i]=ctx.Layout.ByIndex(i).Row<0 ? new Rgb(0,0,255) : new Rgb(0,255,0);}
    }
}
