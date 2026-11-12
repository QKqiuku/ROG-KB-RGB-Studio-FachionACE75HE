using RSS_II_RGB.Core;
using RSS_II_RGB.Core.Device;
using RSS_II_RGB.Core.Input;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class Falchion75Tests
{
    [Fact]
    public void AudioAndMetricBarsResolveSemanticKeysAcrossDifferentAddressMaps()
    {
        var profile = Falchion75Layout.Profile;
        string[] Names(IEnumerable<int> indices) => indices.Select(i => profile.ByIndex(i).Name).ToArray();
        Assert.Equal(new[] { "TAB", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "LEFT_BRACKET", "RIGHT_BRACKET", "ANSI_BACK_SLASH" },
            Names(RSS_II_RGB.App.AudioBarRows.TrebleRow(profile)));
        Assert.Equal(new[] { "CAPS_LOCK", "A", "S", "D", "F", "G", "H", "J", "K", "L", "SEMICOLON", "QUOTE", "ANSI_ENTER" },
            Names(RSS_II_RGB.App.AudioBarRows.MidRow(profile)));
        Assert.Equal(new[] { "LEFT_SHIFT", "Z", "X", "C", "V", "B", "N", "M", "COMMA", "PERIOD", "FORWARD_SLASH", "RIGHT_SHIFT" },
            Names(RSS_II_RGB.App.AudioBarRows.BassRow(profile)));
        var bars = RSS_II_RGB.App.MetricLayouts.Build(RSS_II_RGB.App.MetricLayoutChoice.FunctionRow, profile);
        Assert.Equal(new[] { "F1", "F2", "F3", "F4" }, Names(bars[0].CellIndices));
        Assert.Equal(new[] { "HOME", "END", "INSERT" }, Names(bars[3].CellIndices));
        var diagonal = RSS_II_RGB.App.MetricLayouts.Build(RSS_II_RGB.App.MetricLayoutChoice.Diagonal, profile);
        Assert.Equal(new[] { "M", "J", "U", "7" }, Names(diagonal[0].CellIndices));
    }

    [Fact]
    public void KeyAddressesUseOfficialIndicesWithHardwareVerifiedCorrections()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "falchion75-leds.json");
        using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var expected = json.RootElement.GetProperty("keys").EnumerateArray().ToArray();
        var keys = Falchion75Layout.Profile.Keys.ToArray();
        Assert.Equal(expected.Length, keys.Length);
        foreach (var entry in expected)
        {
            string name = entry.GetProperty("name").GetString()!;
            var actual = Assert.Single(keys, key => key.Name == name);
            byte expectedId = name switch
            {
                "SPACE" => 0x37,
                "PAGE_UP" => 0x75,
                "PAGE_DOWN" => 0x76,
                "ANSI_ENTER" => 0x6D,
                "UP_ARROW" => 0x6E,
                "RIGHT_SHIFT" => 0x66,
                "Z" => 0x16,
                "X" => 0x1E,
                "C" => 0x26,
                "V" => 0x2E,
                "B" => 0x36,
                "N" => 0x3E,
                "M" => 0x46,
                "COMMA" => 0x4E,
                "PERIOD" => 0x56,
                "FORWARD_SLASH" => 0x5E,
                _ => entry.GetProperty("id").GetByte(),
            };
            Assert.Equal(expectedId, actual.KeyId);
        }
    }

    [Theory]
    [InlineData("LEFT_SHIFT", 59, 0x06, 0x2A, false)]
    [InlineData("Z", 60, 0x16, 0x2C, false)]
    [InlineData("X", 61, 0x1E, 0x2D, false)]
    [InlineData("C", 62, 0x26, 0x2E, false)]
    [InlineData("V", 63, 0x2E, 0x2F, false)]
    [InlineData("B", 64, 0x36, 0x30, false)]
    [InlineData("N", 65, 0x3E, 0x31, false)]
    [InlineData("M", 66, 0x46, 0x32, false)]
    [InlineData("COMMA", 67, 0x4E, 0x33, false)]
    [InlineData("PERIOD", 68, 0x56, 0x34, false)]
    [InlineData("FORWARD_SLASH", 69, 0x5E, 0x35, false)]
    [InlineData("RIGHT_SHIFT", 70, 0x66, 0x36, false)]
    [InlineData("UP_ARROW", 71, 0x6E, 0x48, true)]
    [InlineData("PAGE_DOWN", 72, 0x76, 0x51, true)]
    [InlineData("ANSI_ENTER", 57, 0x6D, 0x1C, false)]
    public void OwnerLabelledRowPreservesSelectionInputAndWireAddress(
        string name, int stableIndex, byte physicalAddress, uint scan, bool extended)
    {
        var profile = Falchion75Layout.Profile;
        Assert.Equal(name, profile.ByIndex(stableIndex).Name);
        Assert.Equal(physicalAddress, profile.ByIndex(stableIndex).KeyId);
        Assert.Equal(stableIndex, new ScancodeResolver(profile).ToKeyIndex(scan, extended));
        var pixels = new Rgb[profile.LedCount];
        pixels[stableIndex] = new Rgb(200, 100, 50);
        var frame = new byte[profile.FrameBufferSize];
        DirectProtocol.BuildFrame(profile, pixels, frame);
        int offset = stableIndex / 15 * 65 + 5 + stableIndex % 15 * 4;
        Assert.Equal(new byte[] { physicalAddress, 200, 100, 50 }, frame[offset..(offset + 4)]);
    }

    [Fact]
    public void ExperimentalDeviceRequiresExplicitOptIn()
    {
        Assert.Null(KeyboardProfiles.ForProduct(0x1C7D));
        Assert.Same(Falchion75Layout.Profile, KeyboardProfiles.ForProduct(0x1C7D, true));
        Assert.Null(KeyboardProfiles.ForProduct(0x1C7E, true));
        Assert.Null(KeyboardProfiles.ForProduct(0xFFFF, true));
        Assert.Same(ScopeIILayout.Profile, KeyboardProfiles.ForProduct(0x1AB5));
        Assert.Same(ScopeIILayout.Profile, KeyboardProfiles.ForProduct(0x1AB3));
    }

    [Fact]
    public void CandidateLayoutHasUniqueKeysAndNoNumpadOrLightBar()
    {
        var profile = Falchion75Layout.Profile;
        Assert.Equal(83, profile.LedCount);
        Assert.Equal(83, profile.Keys.ToArray().Select(k => k.KeyId).Distinct().Count());
        Assert.Equal(83, profile.Keys.ToArray().Select(k => (k.Row, k.Col)).Distinct().Count());
        Assert.DoesNotContain(profile.Keys.ToArray(), k => k.Name.StartsWith("NUMPAD") || k.Name == "Logo");
        Assert.Single(profile.Keys.ToArray(), k => k.Name == "SPACE");
        for (int i = 0; i < profile.LedCount; i++)
        {
            var key = profile.ByIndex(i);
            Assert.Equal(i, key.Index);
            Assert.InRange(key.Row, 0, profile.Rows - 1);
            Assert.InRange(key.Col, 0, profile.Cols - 1);
        }
    }

    [Theory]
    [InlineData(0x6A, 0, 13)] // Home
    [InlineData(0x72, 0, 14)] // End
    [InlineData(0x73, 1, 14)] // Insert
    [InlineData(0x74, 2, 14)] // Delete
    [InlineData(0x75, 3, 14)] // Page Up (hardware verified)
    [InlineData(0x76, 4, 14)] // Page Down (hardware verified)
    [InlineData(0x6D, 3, 12)] // Enter (hardware verified)
    [InlineData(0x6E, 4, 13)] // Up arrow (hardware verified)
    [InlineData(0x66, 4, 12)] // Right Shift (hardware verified)
    public void NavigationMatchesM705PhysicalLayout(byte keyId, int row, int col)
    {
        var profile = Falchion75Layout.Profile;
        var key = profile.ByIndex(profile.IndexForKeyId(keyId));
        Assert.Equal((row, col), (key.Row, key.Col));
    }

    [Fact]
    public void RippleAndFadeUse83KeyGeometry()
    {
        var profile = Falchion75Layout.Profile;
        int a = profile.IndexForKeyId(0x0D);
        int s = profile.IndexForKeyId(0x15);
        var pixels = new Rgb[profile.LedCount];
        var hit = new[] { new KeyHit(a, TimeSpan.Zero) };
        var start = new EffectContext(TimeSpan.Zero, TimeSpan.Zero, hit, profile);
        var fade = new KeypressFadeLayer("fade", Rgb.White, 1);
        fade.Render(pixels, start);
        Assert.Equal(Rgb.White, pixels[a]);
        Array.Clear(pixels);
        var ripple = new RippleLayer("ripple", Rgb.White, speedGridPerSec: 4, width: 0.5, fadeSeconds: 1);
        ripple.Render(pixels, start);
        Array.Clear(pixels);
        var next = new EffectContext(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.25), Array.Empty<KeyHit>(), profile);
        ripple.Render(pixels, next);
        Assert.True(pixels[s].R > 0);
        Assert.Equal(Rgb.Black, pixels[a]);
        Array.Clear(pixels);
        var expired = new EffectContext(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), Array.Empty<KeyHit>(), profile);
        fade.Render(pixels, expired);
        Assert.All(pixels, color => Assert.Equal(Rgb.Black, color));
    }

    [Theory]
    [InlineData(0x1E, false, "A")]
    [InlineData(0x20, false, "D")]
    [InlineData(0x39, false, "SPACE")]
    [InlineData(0x48, true, "UP_ARROW")]
    [InlineData(0x53, true, "DELETE")]
    public void ReactiveInputUsesTheSelectedLayout(uint scan, bool extended, string name)
    {
        var profile = Falchion75Layout.Profile;
        int index = new ScancodeResolver(profile).ToKeyIndex(scan, extended);
        Assert.True(index >= 0);
        Assert.Equal(name, profile.ByIndex(index).Name);
        Assert.Equal(-1, new ScancodeResolver(profile).ToKeyIndex(0x47, false));
    }

    [Fact]
    public void CandidateFrameContainsOnlyItsOwnKeysWithCorrectLastPacket()
    {
        var profile = Falchion75Layout.Profile;
        var pixels = new Rgb[profile.LedCount];
        int a = profile.IndexForKeyId(0x0D);
        int d = profile.IndexForKeyId(0x1D);
        pixels[a] = new Rgb(255, 0, 0);
        pixels[d] = new Rgb(0, 0, 255);
        pixels[57] = new Rgb(0, 0, 255); // Enter
        pixels[70] = new Rgb(255, 0, 0); // Right Shift
        pixels[71] = new Rgb(0, 255, 0); // Up arrow
        var frame = new byte[profile.FrameBufferSize];
        DirectProtocol.BuildFrame(profile, pixels, frame);
        Assert.Equal(new byte[] { 0x6D, 0, 0, 255 }, frame[248..252]);
        Assert.Equal(new byte[] { 0x66, 255, 0, 0 }, frame[305..309]);
        Assert.Equal(new byte[] { 0x6E, 0, 255, 0 }, frame[309..313]);
        Assert.Equal(6, profile.PacketsPerFrame);
        Assert.Equal(8, frame[5 * CoreConstants.ReportLength + 3]);
        for (int i = 0; i < profile.LedCount; i++)
        {
            int offset = i / 15 * 65 + 5 + i % 15 * 4;
            Assert.Equal(profile.ByIndex(i).KeyId, frame[offset]);
            Assert.Equal(pixels[i].R, frame[offset + 1]);
            Assert.Equal(pixels[i].G, frame[offset + 2]);
            Assert.Equal(pixels[i].B, frame[offset + 3]);
        }
        Assert.All(frame.Skip(5 * 65 + 5 + 8 * 4), value => Assert.Equal(0, value));
    }
}
