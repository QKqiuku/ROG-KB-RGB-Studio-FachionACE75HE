using RSS_II_RGB.App;
using Avalonia.Media;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Localization;
using RSS_II_RGB.Windows;
using RSS_II_RGB.Core.Rendering;

namespace ROG_KB_RGB_Studio.App.Tests;

public class WorkbenchTests
{
    [Fact]
    public async Task LivePreviewShowsEverySentColourWithoutChangingSelectionOrSavingZones()
    {
        await using var fixture = new EditorFixture();
        var a = fixture.Editor.Keys.Single(k => k.Name == "A");
        var d = fixture.Editor.Keys.Single(k => k.Name == "D");
        a.IsSelected = true;
        var frame = new Rgb[fixture.Editor.Keys.Count];
        frame[a.Index] = new Rgb(255, 0, 0);
        frame[d.Index] = new Rgb(0, 0, 255);
        fixture.Editor.ApplyPreviewFrame(frame);
        Assert.True(fixture.Editor.HasLivePreview);
        Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(a.PreviewBrush).Color);
        Assert.Equal(Colors.Blue, Assert.IsAssignableFrom<ISolidColorBrush>(d.PreviewBrush).Color);
        foreach (var key in fixture.Editor.Keys.Where(k => k != a && k != d))
            Assert.Equal(Colors.Black, Assert.IsAssignableFrom<ISolidColorBrush>(key.PreviewBrush).Color);
        Assert.True(a.IsSelected);
        Assert.False(d.IsSelected);
        Assert.Empty(fixture.Controller.Zones);
        Assert.False(File.Exists(fixture.Path));
        var brush = a.PreviewBrush;
        fixture.Editor.ApplyPreviewFrame(frame.ToArray());
        Assert.Same(brush, a.PreviewBrush);
        frame[a.Index] = Rgb.Black;
        fixture.Editor.ApplyPreviewFrame(frame.ToArray());
        Assert.Equal(Colors.Black, Assert.IsAssignableFrom<ISolidColorBrush>(a.PreviewBrush).Color);
        fixture.Editor.ApplyPreviewFrame(ReadOnlyMemory<Rgb>.Empty);
        Assert.False(fixture.Editor.HasLivePreview);
        Assert.All(fixture.Editor.Keys, k => Assert.Equal(Colors.Black, Assert.IsAssignableFrom<ISolidColorBrush>(k.PreviewBrush).Color));
    }

    [Fact]
    public async Task BarPreviewMirrorsPublishedSourceColoursAndClearsOnDisconnect()
    {
        await using var fixture = new EditorFixture();
        int keyCount = fixture.Editor.Keys.Count;
        foreach (int source in Falchion75Layout.LightBarSourceIndices)
            fixture.Editor.LightBarSegments.Add(new KeyVM(source, "bar", 0, 0));
        var frame = new Rgb[keyCount];
        for (int i = 0; i < frame.Length; i++) frame[i] = new Rgb((byte)i, 80, 160);
        fixture.Editor.ApplyPreviewFrame(frame);
        foreach (var segment in fixture.Editor.LightBarSegments)
            Assert.Equal(Color.FromRgb((byte)segment.Index, 80, 160), Assert.IsAssignableFrom<ISolidColorBrush>(segment.PreviewBrush).Color);
        Assert.Equal(keyCount, fixture.Editor.Keys.Count);
        Assert.Equal(0, fixture.Editor.SelectedCount);
        Assert.False(File.Exists(fixture.Path));
        fixture.Editor.ApplyPreviewFrame(ReadOnlyMemory<Rgb>.Empty);
        Assert.All(fixture.Editor.LightBarSegments, segment => Assert.Equal(Colors.Black, Assert.IsAssignableFrom<ISolidColorBrush>(segment.PreviewBrush).Color));
    }

    [Fact]
    public async Task BarPreviewUsesSentDecorativePixelsRatherThanAssumingMirroring()
    {
        await using var fixture=new EditorFixture();
        foreach(int source in Falchion75Layout.LightBarSourceIndices)fixture.Editor.LightBarSegments.Add(new KeyVM(source,"bar",0,0));
        int keys=fixture.Editor.Keys.Count;
        var frame=new Rgb[keys+13];
        for(int i=0;i<keys;i++)frame[i]=new Rgb(255,0,0);
        for(int i=0;i<13;i++)frame[keys+i]=new Rgb(0,(byte)(40+i),200);
        fixture.Editor.ApplyPreviewFrame(frame);
        Assert.True(fixture.Editor.HasLivePreview);
        Assert.All(fixture.Editor.Keys,key=>Assert.Equal(Colors.Red,Assert.IsAssignableFrom<ISolidColorBrush>(key.PreviewBrush).Color));
        for(int i=0;i<13;i++)Assert.Equal(Color.FromRgb(0,(byte)(40+i),200),Assert.IsAssignableFrom<ISolidColorBrush>(fixture.Editor.LightBarSegments[i].PreviewBrush).Color);
        fixture.Editor.ApplyPreviewFrame(ReadOnlyMemory<Rgb>.Empty);
        Assert.All(fixture.Editor.LightBarSegments,key=>Assert.Equal(Colors.Black,Assert.IsAssignableFrom<ISolidColorBrush>(key.PreviewBrush).Color));
    }

    [Fact]
    public async Task EmptySelectionCannotBeApplied()
    {
        await using var fixture = new EditorFixture();
        Assert.False(fixture.Editor.AssignZoneCommand.CanExecute(null));
    }

    [Fact]
    public async Task ApplyingSelectionSavesOnlyThoseKeysAndRestoresThemOnReload()
    {
        await using var fixture = new EditorFixture();
        var keys = fixture.Editor.Keys.Where(k => k.Name is "A" or "D").ToArray();
        foreach (var key in keys) key.IsSelected = true;
        fixture.Editor.ZoneColor = Color.FromRgb(12, 34, 56);
        fixture.Editor.AssignZoneCommand.Execute(null);
        var saved = Assert.Single(new SettingsService(fixture.Path).Settings.Zones);
        Assert.Equal(keys.Select(k => k.Index), saved.KeyIndices);
        Assert.Equal("0C2238", saved.ColorHex);
        Assert.All(fixture.Editor.Keys, k => Assert.False(k.IsSelected));
        Assert.Single(fixture.Controller.Zones);
    }

    [Fact]
    public async Task SpectrumAndHexColourStayInSync()
    {
        await using var fixture = new EditorFixture();
        fixture.Editor.ZoneColorHex = "#CC5533";
        Assert.Equal(fixture.Editor.ZoneColor, fixture.Editor.ZoneHsvColor.ToRgb());
        fixture.Editor.ZoneHsvColor = Colors.Blue.ToHsv();
        Assert.Equal("#0000FF", fixture.Editor.ZoneColorHex);
        Assert.Equal(Colors.Blue, fixture.Editor.ZoneColor);
    }

    [Fact]
    public async Task InvalidColourCannotApplyAndAValidHexRecovers()
    {
        await using var fixture = new EditorFixture();
        fixture.Editor.Keys.Single(k => k.Name == "A").IsSelected = true;
        Assert.Equal(1, fixture.Editor.SelectedCount);
        fixture.Editor.ZoneColorHex = null!;
        Assert.True(fixture.Editor.HasColorError);
        fixture.Editor.ZoneColorHex = "broken";
        Assert.True(fixture.Editor.HasColorError);
        Assert.False(fixture.Editor.AssignZoneCommand.CanExecute(null));
        fixture.Editor.ZoneColorHex = "#123456";
        Assert.False(fixture.Editor.HasColorError);
        Assert.True(fixture.Editor.AssignZoneCommand.CanExecute(null));
        Assert.Equal(Color.FromRgb(0x12, 0x34, 0x56), fixture.Editor.ZoneColor);
        fixture.Editor.SelectNoneCommand.Execute(null);
        Assert.Empty(fixture.Editor.SelectedKeys);
        Assert.False(fixture.Editor.AssignZoneCommand.CanExecute(null));
    }

    [Fact]
    public async Task EditingAZoneUpdatesItInsteadOfAddingDuplicates()
    {
        await using var fixture = new EditorFixture();
        fixture.Editor.Keys.Single(k => k.Name == "A").IsSelected = true;
        fixture.Editor.AssignZoneCommand.Execute(null);
        fixture.Editor.ZoneColorHex = "invalid";
        fixture.Editor.ZoneRows[0].EditCommand.Execute(null);
        Assert.False(fixture.Editor.HasColorError);
        Assert.True(fixture.Editor.IsEditing);
        Assert.Equal("A", Assert.Single(fixture.Editor.SelectedKeys).Name);
        fixture.Editor.ZoneEffect = EffectChoice.Breathing;
        fixture.Editor.ZoneColorHex = "#334455";
        fixture.Editor.AssignZoneCommand.Execute(null);
        Assert.Single(fixture.Editor.ZoneRows);
        var zone = Assert.Single(new SettingsService(fixture.Path).Settings.Zones);
        Assert.Equal(EffectChoice.Breathing, zone.Effect);
        Assert.Equal("334455", zone.ColorHex);
        Assert.False(fixture.Editor.IsEditing);
        fixture.Editor.ZoneRows[0].RemoveCommand.Execute(null);
        Assert.Empty(fixture.Controller.Zones);
        Assert.Empty(new SettingsService(fixture.Path).Settings.Zones);
    }

    [Fact]
    public void FalchionGeometryHasNoOverlapsAndSpaceIsFullWidth()
    {
        var profile = Falchion75Layout.Profile;
        var bounds = profile.Keys.ToArray().Select(k => KeyboardGeometry.ForKey(profile, k)).ToArray();
        Assert.Equal(83, bounds.Length);
        foreach (var box in bounds)
        {
            Assert.True(box.Width > 0 && box.Height > 0);
            Assert.InRange(box.X, 0, KeyboardGeometry.Width(profile) - box.Width);
            Assert.InRange(box.Y, 0, KeyboardGeometry.Height(profile) - box.Height);
        }
        for (int a = 0; a < bounds.Length; a++)
        for (int b = a + 1; b < bounds.Length; b++)
        {
            var x = bounds[a]; var y = bounds[b];
            Assert.False(x.X < y.X + y.Width && x.X + x.Width > y.X && x.Y < y.Y + y.Height && x.Y + x.Height > y.Y,
                $"Keys {a} and {b} overlap.");
        }
        var space = profile.ByIndex(profile.IndexForKeyName("SPACE"));
        Assert.True(KeyboardGeometry.ForKey(profile, space).Width > KeyboardGeometry.Unit * 6);
        Assert.Equal(6, space.Col);
    }

    [Theory]
    [InlineData(0x0804, AppLanguage.SimplifiedChinese)]
    [InlineData(0x1004, AppLanguage.SimplifiedChinese)]
    [InlineData(0x0404, AppLanguage.TraditionalChinese)]
    [InlineData(0x0C04, AppLanguage.TraditionalChinese)]
    [InlineData(0x0409, AppLanguage.English)]
    public void WindowsLocaleSelectsTheExpectedLanguage(ushort languageId, AppLanguage expected)
        => Assert.Equal(expected, Win32UiLanguageProvider.FromLanguageId(languageId));

    private sealed class EditorFixture : IAsyncDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RSS_II_RGB-tests", Guid.NewGuid().ToString("N"), "settings.json");
        public KeyboardController Controller { get; } = new(new QuietLog(), new SensorState());
        public ZoneEditorViewModel Editor { get; }
        public EditorFixture() => Editor = new ZoneEditorViewModel(Controller, new SettingsService(Path));
        public async ValueTask DisposeAsync()
        {
            await Controller.DisposeAsync();
            File.Delete(Path);
            Directory.Delete(System.IO.Path.GetDirectoryName(Path)!);
        }
    }

    private sealed class QuietLog : ILogSink
    {
        public void Log(LogLevel level, string message, Exception? exception = null) { }
    }
}
