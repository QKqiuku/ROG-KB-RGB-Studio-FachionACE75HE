using System.Text.Json;
using RSS_II_RGB.Core.Animation;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class AtkSceneTests
{
    private static AtkScene Parse(string effects, string animations = "[]", bool fit = false, bool autoLayout = false)
    {
        using var json = JsonDocument.Parse("{\"version\":1,\"animations\":" + animations + ",\"effects\":" + effects + ",\"settings\":{\"autoLayout\":" + (autoLayout ? "true" : "false") + "}}");
        return AtkScene.Parse(json.RootElement, Falchion75Layout.Profile, fit);
    }

    [Theory]
    [InlineData("amplitude_bars")]
    [InlineData("stereo_spectrum")]
    [InlineData("AudioFrequency")]
    public void AudioSmoothingUsesElapsedTimeAcrossFrameRates(string type)
    {
        Rgb[] RenderAt(int fps)
        {
            bool module=type=="AudioFrequency";
            var scene=Parse(module?"[]":"[{\"type\":\""+type+"\",\"config\":{}}]",
                module?"""[{"type":"AudioFrequency","fill":"#FFFFFF","params":{"count":15,"width":13,"gap":0}}]""":"[]",autoLayout:module);
            var pixels=new Rgb[83];
            var loud=new RSS_II_RGB.Core.Audio.AudioFrame {TimestampMs=Environment.TickCount64,Generation=1,Spectrum=Enumerable.Repeat(1d,32).ToArray(),Bass=1,Mid=1,Treble=1};
            scene.Render(pixels,TimeSpan.Zero,loud);
            var quiet=loud with {Spectrum=Enumerable.Repeat(.2,32).ToArray(),Bass=.2,Mid=.2,Treble=.2};
            for(int i=1;i<=fps/5;i++) scene.Render(pixels,TimeSpan.FromSeconds((double)i/fps),quiet);
            return pixels;
        }
        var slow=RenderAt(15);var fast=RenderAt(60);
        for(int i=0;i<slow.Length;i++)
        {
            Assert.InRange(Math.Abs(slow[i].R-fast[i].R),0,1);
            Assert.InRange(Math.Abs(slow[i].G-fast[i].G),0,1);
            Assert.InRange(Math.Abs(slow[i].B-fast[i].B),0,1);
        }
    }

    [Theory]
    [InlineData(144,470,1,4,25)]
    [InlineData(204.8984375,412,5,1,9)]
    public void PublicAbsoluteCanvasExportsFailExplicitlyWithoutDevicePlacement(double x,double y,int width,int gap,int kernel)
    {
        // Geometry from public presets 683 and 2827; neither includes device canvas placement.
        string animations=System.Text.Json.JsonSerializer.Serialize(new[]{new {type="AudioFrequency",visible=true,fill="#722ED1",@params=new {},data=new {pos=new{x,y},count=64,width,gap,kernelSize=kernel}}});
        var error=Assert.Throws<InvalidDataException>(()=>Parse("[]",animations));
        Assert.Contains("device canvas placement",error.Message);
    }

    [Fact]
    public void NativeModuleUsesBoundedCanvasParametersAndGradient()
    {
        var scene=Parse("[]", """[{"type":"AudioFrequency","visible":true,"fill":{"type":"linear","stops":[{"offset":0,"color":"#FF0000"},{"offset":1,"color":"#0000FF"}]},"params":{"count":15,"width":13,"gap":0,"scale":20,"pos":{"x":0,"y":300},"rate":25,"kernelSize":3}}]""",autoLayout:true);
        Assert.True(scene.UsesAudio);
        var pixels=new Rgb[83];
        scene.Render(pixels,TimeSpan.Zero,new RSS_II_RGB.Core.Audio.AudioFrame { TimestampMs=Environment.TickCount64,Spectrum=Enumerable.Repeat(1d,32).ToArray() });
        Assert.Contains(pixels,p=>p!=Rgb.Black);
        Assert.Throws<InvalidDataException>(()=>Parse("[]", """[{"type":"AudioFrequency","params":{"count":999999}}]"""));
    }

    [Fact]
    public void NamedSpectrumAndPulseKeepTheirUpstreamTimeBasedBehavior()
    {
        foreach(string type in new[]{"spectrum","pulse"})
        {
            var scene=Parse("[{\"type\":\""+type+"\",\"config\":{}}]");
            Assert.False(scene.UsesAudio);
            var pixels=new Rgb[83];
            scene.Render(pixels,TimeSpan.FromMilliseconds(100));
            Assert.Contains(pixels,p=>p!=Rgb.Black);
        }
    }

    [Fact]
    public void AmplitudeBarsMixWithKeyframesAndUseRealSpectrum()
    {
        var scene = Parse("""[{"type":"amplitude_bars","config":{"colors":["#00FF00"]}},{"type":"keyframe","config":{"customParams":{"frames":[{"3_1":"#FF0000"}]}}}]""");
        var pixels = new Rgb[83];
        Assert.True(scene.UsesAudio);
        scene.Render(pixels, TimeSpan.Zero);
        Assert.Equal(new Rgb(255,0,0),pixels[46]);
        scene.Render(pixels, TimeSpan.FromMilliseconds(16), new RSS_II_RGB.Core.Audio.AudioFrame {TimestampMs=Environment.TickCount64, Spectrum=Enumerable.Repeat(1d,32).ToArray()});
        Assert.Equal(new Rgb(255,255,0),pixels[46]);
    }

    [Fact]
    public void StaleAudioCannotLightNewBarsAndHiddenUnsupportedModulesAreSkipped()
    {
        var scene=Parse("""[{"type":"amplitude_bars","config":{}}]""", """[{"type":"Particles","visible":false}]""");
        var pixels=new Rgb[83];
        scene.Render(pixels,TimeSpan.Zero,new RSS_II_RGB.Core.Audio.AudioFrame {TimestampMs=1,Spectrum=[1,1,1]});
        Assert.All(pixels,p=>Assert.Equal(Rgb.Black,p));
    }

    [Theory]
    [InlineData("stereo_spectrum")]
    [InlineData("amplitude_bars")]
    public void AudioGenerationChangeClearsHistory(string type)
    {
        var scene=Parse("[{\"type\":\""+type+"\",\"config\":{}}]");
        var pixels=new Rgb[83];
        scene.Render(pixels,TimeSpan.Zero,new RSS_II_RGB.Core.Audio.AudioFrame {TimestampMs=Environment.TickCount64,Generation=1,Bass=1,Mid=1,Treble=1,Spectrum=[1,1,1]});
        scene.Render(pixels,TimeSpan.FromMilliseconds(16),new RSS_II_RGB.Core.Audio.AudioFrame {TimestampMs=Environment.TickCount64,Generation=2});
        Assert.All(pixels,p=>Assert.Equal(Rgb.Black,p));
    }

    [Fact]
    public void SparseFramesKeepTimingLoopAndTransparentBlackDistinct()
    {
        var scene = Parse("""[{"type":"keyframe","config":{"speed":1,"brightness":100,"opacity":1,"customParams":{"frames":[{"3_1":"#FF0000"},{"3_3":"#0000FF"}],"frameDuration":100,"loop":true}}}]""");
        var pixels = new Rgb[83];
        scene.Render(pixels, TimeSpan.FromMilliseconds(99));
        Assert.Equal(new Rgb(255, 0, 0), pixels[46]);
        Assert.Equal(Rgb.Black, pixels[48]);
        scene.Render(pixels, TimeSpan.FromMilliseconds(100));
        Assert.Equal(Rgb.Black, pixels[46]);
        Assert.Equal(new Rgb(0, 0, 255), pixels[48]);
        scene.Render(pixels, TimeSpan.FromMilliseconds(200));
        Assert.Equal(new Rgb(255, 0, 0), pixels[46]);
    }

    [Fact]
    public void IndependentLayersUseAtkScreenBlendVariableDelaysAndFinalHold()
    {
        var scene = Parse("""[{"type":"keyframe","config":{"customParams":{"frames":[{"3_1":"#FF0000","3_3":"#FF0000"}],"frameDuration":100}}},{"type":"keyframe","config":{"speed":2,"brightness":50,"opacity":0.5,"customParams":{"frames":[{"3_1":"#0000FF"},{"3_1":"#00FF00"}],"frameDelays":[100,200],"loop":false}}}]""");
        var pixels = new Rgb[83];
        scene.Render(pixels, TimeSpan.Zero);
        Assert.Equal(new Rgb(255, 0, 128), pixels[46]);
        Assert.Equal(new Rgb(255, 0, 0), pixels[48]);
        scene.Render(pixels, TimeSpan.FromMilliseconds(50));
        Assert.Equal(new Rgb(255, 128, 0), pixels[46]);
        scene.Render(pixels, TimeSpan.FromSeconds(10));
        Assert.Equal(new Rgb(255, 128, 0), pixels[46]);
    }

    [Fact]
    public void AtkScreenBlendKeepsLowerColoursUnderBlackAndSkipsZeroOpacityLayers()
    {
        var scene = Parse("""[{"type":"keyframe","config":{"customParams":{"frames":[{"3_1":"#FF0000"}]}}},{"type":"keyframe","config":{"customParams":{"frames":[{"3_1":"#000000"}]}}},{"type":"keyframe","config":{"opacity":0,"customParams":{"frames":[{"3_1":"#00FF00"}]}}}]""");
        var pixels = new Rgb[83];
        scene.Render(pixels, TimeSpan.Zero);
        Assert.Equal(new Rgb(255, 0, 0), pixels[46]);
    }

    [Fact]
    public void FitUsesSourceDimensionsWhilePreserveKeepsOriginalCoordinates()
    {
        const string effects = """[{"type":"keyframe","config":{"customParams":{"sourceRows":1,"sourceCols":1,"frames":[{"0_0":"#123456"}]}}}]""";
        var pixels = new Rgb[83];
        Parse(effects, fit: true).Render(pixels, TimeSpan.Zero);
        Assert.All(pixels, p => Assert.Equal(new Rgb(0x12, 0x34, 0x56), p));
        Parse(effects).Render(pixels, TimeSpan.Zero);
        Assert.Equal(new Rgb(0x12, 0x34, 0x56), pixels[0]);
        Assert.Equal(1, pixels.Count(p => !p.Equals(Rgb.Black)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeventyFiveGridMapsWholeLowerRowAndWideKeysWithoutChangingHidAddresses(bool fit)
    {
        var frame = new Dictionary<string, string>();
        for (int r = 0; r < 6; r++)
        for (int c = 0; c < 15; c++) frame[$"{r}_{c}"] = $"#{r * 30 + 10:X2}{c * 10 + 5:X2}80";
        // RS7 animation cells reserved for wide keys / gaps, not LEDs.
        foreach (string gap in new[] { "3_12", "4_12", "4_14", "5_11" }) frame.Remove(gap);
        string effects = "[{\"type\":\"keyframe\",\"config\":{\"customParams\":{\"sourceRows\":6,\"sourceCols\":15,\"frames\":[" + JsonSerializer.Serialize(frame) + "]}}}]";
        var pixels = new Rgb[83];
        Parse(effects, fit: fit).Render(pixels, TimeSpan.Zero);
        var profile = Falchion75Layout.Profile;
        foreach (var key in profile.Keys)
        {
            int row = key.Row, col = key.Col;
            if (key.Name == "ANSI_ENTER") col = 13;
            if (key.Row == 4 && key.Col is >= 2 and <= 11) col--;
            if (key.Name == "RIGHT_SHIFT") col = 11;
            if (key.Name == "PAGE_DOWN") row = 3;
            if (key.Name == "RIGHT_CONTROL") col = 12;
            if (key.Name == "SPACE") col = 5;
            Assert.Equal(new Rgb((byte)(row * 30 + 10), (byte)(col * 10 + 5), 128), pixels[key.Index]);
        }
    }

    [Fact]
    public void MissingKeyFallbackDoesNotOverrideExplicitBlackOrFillUnpaintedArtwork()
    {
        var scene = Parse("""[{"type":"keyframe","config":{"customParams":{"sourceRows":6,"sourceCols":15,"frames":[{"4_14":"#000000","3_14":"#FF0000","5_11":"#000000","5_12":"#00FF00"},{}]}}}]""");
        var pixels = new Rgb[83];
        scene.Render(pixels, TimeSpan.Zero);
        Assert.Equal(Rgb.Black, pixels[72]);
        Assert.Equal(Rgb.Black, pixels[79]);
        scene.Render(pixels, TimeSpan.FromMilliseconds(200));
        Assert.All(pixels, color => Assert.Equal(Rgb.Black, color));
    }

    [Fact]
    public void RebuildingTheSameSceneDoesNotRestartItsPlayback()
    {
        var scene = Parse("""[{"type":"keyframe","config":{"customParams":{"frames":[{"3_1":"#FF0000"},{"3_1":"#0000FF"}],"frameDuration":100}}}]""");
        var pixels = new Rgb[83];
        new AtkSceneLayer("first", scene, KeyMask.All).Render(pixels,
            new EffectContext(TimeSpan.FromMilliseconds(1000), TimeSpan.Zero, [], Falchion75Layout.Profile));
        Assert.Equal(new Rgb(255, 0, 0), pixels[46]);
        new AtkSceneLayer("rebuilt", scene, KeyMask.All).Render(pixels,
            new EffectContext(TimeSpan.FromMilliseconds(1150), TimeSpan.Zero, [], Falchion75Layout.Profile));
        Assert.Equal(new Rgb(0, 0, 255), pixels[46]);
        new AtkSceneLayer("reconnected", scene, KeyMask.All).Render(pixels,
            new EffectContext(TimeSpan.Zero, TimeSpan.Zero, [], Falchion75Layout.Profile));
        Assert.Equal(new Rgb(255, 0, 0), pixels[46]);
        new AtkSceneLayer("after-reconnect", scene, KeyMask.All).Render(pixels,
            new EffectContext(TimeSpan.FromMilliseconds(150), TimeSpan.Zero, [], Falchion75Layout.Profile));
        Assert.Equal(new Rgb(0, 0, 255), pixels[46]);
    }

    [Theory]
    [InlineData("[{\"type\":\"fps_radar\",\"config\":{}}]", "[]")]
    [InlineData("[]", "[{\"type\":\"audio\"}]")]
    [InlineData("[{\"type\":\"keyframe\",\"config\":{\"customParams\":{\"frames\":[{\"3_1\":\"#GGGGGG\"}]}}}]", "[]")]
    [InlineData("[{\"type\":\"keyframe\",\"config\":{\"customParams\":{\"frames\":[{\"999_0\":\"#FFFFFF\"}]}}}]", "[]")]
    [InlineData("[{\"type\":\"keyframe\",\"config\":{\"customParams\":{\"frames\":[{}],\"frameDuration\":-1}}}]", "[]")]
    [InlineData("[{\"type\":\"keyframe\",\"config\":{\"customParams\":{\"frames\":[{},{}],\"frameDelays\":[100]}}}]", "[]")]
    public void UnsupportedAndMalformedScenesAreRejected(string effects, string animations)
        => Assert.Throws<InvalidDataException>(() => Parse(effects, animations));
}
