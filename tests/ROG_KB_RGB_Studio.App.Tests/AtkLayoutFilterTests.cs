using RSS_II_RGB.App;
using System.Text.Json;
using RSS_II_RGB.Core.Layout;

namespace ROG_KB_RGB_Studio.App.Tests;

public class AtkLayoutFilterTests
{
    [Theory]
    [InlineData("RS7 Air", 0, 0, true)]
    [InlineData("ATKRS7 V2 animation", 0, 0, true)]
    [InlineData("EDGE75", 0, 0, true)]
    [InlineData("75% keyboard Heart", 0, 0, true)]
    [InlineData("75% Heart", 0, 0, false)]
    [InlineData("亮度75%的爱心", 0, 0, false)]
    [InlineData("75配列", 0, 0, true)]
    [InlineData("Heart", 6, 15, true)]
    [InlineData("RS6air Heart", 0, 0, false)]
    [InlineData("RS8 Air", 0, 0, false)]
    [InlineData("ATK68RX", 0, 0, false)]
    [InlineData("RS7", 5, 15, false)]
    [InlineData("RS6", 6, 15, false)]
    [InlineData("RS7 RS6", 0, 0, false)]
    [InlineData("Heart", 0, 0, false)]
    [InlineData("75FPS animation", 0, 0, false)]
    [InlineData("Universal", 0, 0, false)]
    public void OnlyUnambiguousMatchingLayoutsAreListed(string title, int rows, int cols, bool expected)
        => Assert.Equal(expected, AtkLayoutFilter.Matches(Preset(title, rows, cols), Falchion75Layout.Profile));

    [Fact]
    public void SparseColouredBoundsDoNotProveTheSourceLayout()
    {
        using var json = JsonDocument.Parse("""{"version":1,"effects":[{"type":"keyframe","config":{"customParams":{"frames":[{"5_14":"#FF0000"}]}}}]}""");
        Assert.False(AtkLayoutFilter.Matches(new AtkPreset("1", "Heart", "Creator", "", json.RootElement.Clone()), Falchion75Layout.Profile));
    }

    [Fact]
    public void MatchingFollowsTheCurrentProfileRatherThanAssumingSeventyFivePercent()
    {
        Assert.Equal(KeyboardFormFactor.Compact75, Falchion75Layout.Profile.FormFactor);
        Assert.Equal(KeyboardFormFactor.FullSize, ScopeIILayout.Profile.FormFactor);
        Assert.False(AtkLayoutFilter.Matches(Preset("RS7"), ScopeIILayout.Profile));
        Assert.True(AtkLayoutFilter.Matches(Preset("104键 Heart"), ScopeIILayout.Profile));
        var future = new KeyboardProfile("Future ROG", 5, 15, [new(1, "ESCAPE", 0, 0, 0)], KeyboardFormFactor.Compact65);
        Assert.True(AtkLayoutFilter.Matches(Preset("RS6 Air"), future));
        Assert.False(AtkLayoutFilter.Matches(Preset("RS7 Air"), future));
        var unspecified = new KeyboardProfile("Unknown ROG", 6, 15, [new(1, "ESCAPE", 0, 0, 0)]);
        Assert.False(AtkLayoutFilter.Matches(Preset("RS7 Air"), unspecified));
    }

    internal static AtkPreset Preset(string title, int rows = 0, int cols = 0)
    {
        string dimensions = rows > 0 ? $",\"sourceRows\":{rows},\"sourceCols\":{cols}" : "";
        using var json = JsonDocument.Parse("{\"version\":1,\"effects\":[{\"type\":\"keyframe\",\"config\":{\"customParams\":{\"frames\":[{\"0_0\":\"#FF0000\"}]" + dimensions + "}}}]}");
        return new AtkPreset("1", title, "Creator", "", json.RootElement.Clone());
    }

    [Fact]
    public void SourceModelAndAllLayerDimensionsMustAgreeWithTheTarget()
    {
        Assert.True(AtkLayoutFilter.Matches(Preset("Heart") with { DeviceName = "ATK RS7" }, Falchion75Layout.Profile));
        Assert.False(AtkLayoutFilter.Matches(Preset("RS7") with { DeviceName = "RS6" }, Falchion75Layout.Profile));
        Assert.True(AtkLayoutFilter.Matches(Preset("Heart") with { Description = "For RS7" }, Falchion75Layout.Profile));
        Assert.False(AtkLayoutFilter.Matches(Preset("Heart") with { Description = "RS6 and RS7" }, Falchion75Layout.Profile));
        Assert.False(AtkLayoutFilter.Matches(Preset("Heart") with { Description = "建议亮度75%" }, Falchion75Layout.Profile));
        using var mixed = JsonDocument.Parse("""{"effects":[{"config":{"customParams":{"sourceRows":6,"sourceCols":15}}},{"config":{"customParams":{"sourceRows":5,"sourceCols":15}}}]}""");
        Assert.False(AtkLayoutFilter.Matches(Preset("RS7") with { Scene = mixed.RootElement.Clone() }, Falchion75Layout.Profile));
        using var partial = JsonDocument.Parse("""{"effects":[{"config":{"customParams":{"sourceRows":6}}}]}""");
        Assert.False(AtkLayoutFilter.Matches(Preset("RS7") with { Scene = partial.RootElement.Clone() }, Falchion75Layout.Profile));
    }
}
