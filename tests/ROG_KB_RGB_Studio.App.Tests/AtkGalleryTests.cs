using RSS_II_RGB.App;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RSS_II_RGB.Core.Animation;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;

namespace ROG_KB_RGB_Studio.App.Tests;

public class AtkGalleryTests
{
    private const string Response = """{"statusCode":200,"data":{"total":1,"list":[{"configurationBaseManagement":{"configurationId":"528","configurationName":"Heart","deviceName":"104-key","scenePreset":{"version":1,"animations":[],"effects":[{"type":"keyframe","config":{"customParams":{"frames":[{"0_0":"#FF0000"}]}}}]}},"cloudConfigurationSquare":{"configurationCardInfo":{"authorNickname":"Creator"}},"configurationSharing":{"configurationDescription":"Shared animation"}}]}}""";

    [Fact]
    public async Task PublicListUsesOnlyAnonymousReadQueryAndKeepsSceneAndAttribution()
    {
        using var http = new HttpClient(new Handler(async request =>
        {
            Assert.Equal("https://api-hub.vxe.com/v1/share/config/list", request.RequestUri!.AbsoluteUri);
            Assert.Null(request.Headers.Authorization);
            Assert.Equal(HttpMethod.Post, request.Method);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal("music_rhythm", body.RootElement.GetProperty("configType").GetString());
            Assert.Equal("square", body.RootElement.GetProperty("type").GetString());
            Assert.Equal("latest", body.RootElement.GetProperty("sort").GetString());
            Assert.Equal("a\"b", body.RootElement.GetProperty("keyword").GetString());
            Assert.Equal(20, body.RootElement.GetProperty("pageSize").GetInt32());
            return Reply(Response);
        }));
        var page = await new AtkGalleryClient(http).GetPageAsync("a\"b", true, 1, TestContext.Current.CancellationToken);
        var item = Assert.Single(page.Items);
        Assert.Equal("528", item.Id);
        Assert.Equal("Heart", item.Title);
        Assert.Equal("Creator", item.Author);
        Assert.Equal("104-key", item.DeviceName);
        Assert.Equal("Shared animation", item.Description);
        Assert.True(item.IsKeyframe);
        Assert.Equal(1, item.Scene.GetProperty("effects").GetArrayLength());
    }

    [Theory]
    [InlineData("{\"statusCode\":401}")]
    [InlineData("{\"statusCode\":200,\"data\":{\"total\":1,\"list\":null}}")]
    [InlineData("not json")]
    public async Task MalformedOrRejectedResponsesFailWithoutApplyingAnything(string response)
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Reply(response))));
        await Assert.ThrowsAnyAsync<Exception>(() => new AtkGalleryClient(http).GetPageAsync("", false, 1, TestContext.Current.CancellationToken));
    }

    private static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task PreviewDoesNotChangeSettingsAndImportPreservesGifZonesAndFrameRateAcrossRestart()
    {
        string folder = Path.Combine(Path.GetTempPath(), "RSS_II_RGB-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "settings.json");
        string oldGif = Path.Combine(folder, "original.kbanim");
        File.WriteAllText(oldGif, "original GIF cache");
        await using var controller = new KeyboardController(new QuietLog(), new SensorState());
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Reply(Response))));
        try
        {
            var store = new SettingsService(path);
            store.Settings.GlobalEffect = EffectChoice.Solid;
            store.Settings.TargetFps = 30;
            store.Settings.GifAnimPath = oldGif;
            store.Settings.GifSourcePath = "original.gif";
            store.Settings.Zones = [new ZoneSetting { KeyIndices = [0], ColorHex = "0000FF" }];
            var main = new MainViewModel(controller, store, new NoStartup());
            main.RestoreSettings();
            string before = File.ReadAllText(path);
            var client = new AtkGalleryClient(http);
            using (var gallery = new AtkGalleryViewModel(main, client))
            {
                await gallery.LoadAsync();
                gallery.SelectedItem = Assert.Single(gallery.Items);
                await gallery.PreparePreviewAsync();
                gallery.RenderPreview();
                Assert.True(gallery.CanApply);
                Assert.Equal(before, File.ReadAllText(path));
                Assert.Equal(EffectChoice.Solid, main.SelectedEffect);
            }
            var preset = Assert.Single((await client.GetPageAsync("", false, 1, TestContext.Current.CancellationToken)).Items);
            string beforeImport = File.ReadAllText(path);
            await Assert.ThrowsAsync<InvalidDataException>(() => main.ImportAtkAsync(preset with { DeviceName = "RS7" }, AtkScene.Parse(preset.Scene, controller.Profile, false), true, folder));
            Assert.Equal(beforeImport, File.ReadAllText(path));
            await main.ImportAtkAsync(preset, AtkScene.Parse(preset.Scene, controller.Profile, false), false, folder);
            Assert.False(main.IsCustomMode);
            Assert.Equal(EffectChoice.Atk, main.SelectedEffect);
            var saved = new SettingsService(path).Settings;
            Assert.Equal(30, saved.TargetFps);
            Assert.Equal("Creator", saved.AtkPresetAuthor);
            Assert.Equal("528", saved.AtkPresetId);
            Assert.Equal(EffectChoice.Solid, saved.CustomBaseEffect);
            Assert.Single(saved.Zones);
            Assert.Equal(oldGif, saved.GifAnimPath);
            Assert.Equal("original GIF cache", File.ReadAllText(oldGif));
            var restored = new MainViewModel(controller, new SettingsService(path), new NoStartup());
            restored.RestoreSettings();
            Assert.Equal(EffectChoice.Atk, restored.SelectedEffect);
            Assert.Contains("Heart", restored.AtkStatusText);
            var compositor = new Compositor(controller.Profile.LedCount);
            foreach (var layer in controller.BuildLayers()) compositor.Add(layer);
            var frame = new LedFrame(controller.Profile.LedCount);
            compositor.Compose(frame, new EffectContext(TimeSpan.Zero, TimeSpan.Zero, [], controller.Profile));
            Assert.Equal(new Rgb(255, 0, 0), frame.Pixels[0]);
        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
    }

    [Fact]
    public async Task OversizedResponsesAreRejectedBeforeParsing()
    {
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Reply(new string(' ', AppConstants.AtkMaxResponseBytes + 1)))));
        await Assert.ThrowsAsync<InvalidDataException>(() => new AtkGalleryClient(http).GetPageAsync("", false, 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GalleryHidesOtherAndUnknownLayoutsAcrossTheWholeCatalogue()
    {
        var json = JsonNode.Parse(Response)!;
        var data = json["data"]!;
        data["total"] = 120;
        var list = data["list"]!.AsArray();
        var wrong = list[0]!.DeepClone();
        wrong["configurationBaseManagement"]!["configurationId"] = "2";
        wrong["configurationBaseManagement"]!["deviceName"] = "RS7";
        list.Add(wrong);
        var unknown = list[0]!.DeepClone();
        unknown["configurationBaseManagement"]!["configurationId"] = "3";
        unknown["configurationBaseManagement"]!["deviceName"] = "";
        list.Add(unknown);
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Reply(json.ToJsonString()))));
        string folder = Path.Combine(Path.GetTempPath(), "RSS_II_RGB-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await using var controller = new KeyboardController(new QuietLog(), new SensorState());
        try
        {
            var main = new MainViewModel(controller, new SettingsService(Path.Combine(folder, "settings.json")), new NoStartup());
            main.RestoreSettings();
            using var gallery = new AtkGalleryViewModel(main, new AtkGalleryClient(http));
            await gallery.LoadAsync();
            Assert.Equal("528", Assert.Single(gallery.Items).Id);
            gallery.SelectedItem = new AtkPreset("2", "RS7", "Creator", "", AtkLayoutFilterTests.Preset("RS7").Scene);
            await gallery.PreparePreviewAsync();
            Assert.False(gallery.CanApply);
            Assert.False(await gallery.ApplyAsync());
        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
    }

    [Theory]
    [InlineData(0, "hot")]
    [InlineData(1, "latest")]
    public async Task GalleryLoadsAllRemotePagesInRequestedOrder(int sort, string order)
    {
        var requested = new List<int>();
        using var http = new HttpClient(new Handler(async request =>
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(order, body.RootElement.GetProperty("sort").GetString());
            int page = body.RootElement.GetProperty("current").GetInt32();
            requested.Add(page);
            var json = JsonNode.Parse(Response)!;
            json["data"]!["total"] = 140;
            var info = json["data"]!["list"]![0]!["configurationBaseManagement"]!;
            info["configurationId"] = page.ToString();
            // The first remote page has no matching layout.
            info["deviceName"] = page == 1 ? "RS7" : "104-key";
            return Reply(json.ToJsonString());
        }));
        string folder = Path.Combine(Path.GetTempPath(), "RSS_II_RGB-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await using var controller = new KeyboardController(new QuietLog(), new SensorState());
        try
        {
            var main = new MainViewModel(controller, new SettingsService(Path.Combine(folder, "settings.json")), new NoStartup());
            main.RestoreSettings();
            using var gallery = new AtkGalleryViewModel(main, new AtkGalleryClient(http));
            gallery.SortIndex = sort;
            await gallery.LoadAsync();
            Assert.Equal(new[] { "2", "3", "4", "5", "6", "7" }, gallery.Items.Select(p => p.Id));
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, requested);
            Assert.False(gallery.IsBusy);

        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
    }

    [Theory]
    [InlineData("network")]
    [InlineData("empty")]
    [InlineData("duplicate")]
    public async Task CatalogueKeepsPartialMatchesAndNeverReportsFailuresAsComplete(string scenario)
    {
        int requests = 0;
        using var http = new HttpClient(new Handler(_ =>
        {
            requests++;
            var json = JsonNode.Parse(Response)!;
            json["data"]!["total"] = 40;
            if (requests == 2)
            {
                if (scenario == "network") throw new HttpRequestException("offline");
                if (scenario == "empty") json["data"]!["list"] = new JsonArray();
            }
            return Task.FromResult(Reply(json.ToJsonString()));
        }));
        string folder = Path.Combine(Path.GetTempPath(), "RSS_II_RGB-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await using var controller = new KeyboardController(new QuietLog(), new SensorState());
        try
        {
            var main = new MainViewModel(controller, new SettingsService(Path.Combine(folder, "settings.json")), new NoStartup());
            main.RestoreSettings();
            using var gallery = new AtkGalleryViewModel(main, new AtkGalleryClient(http));
            await gallery.LoadAsync();
            Assert.Equal(2, requests);
            Assert.Equal("528", Assert.Single(gallery.Items).Id);
            Assert.False(gallery.IsBusy);
            if (scenario != "duplicate")
                Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, L.AtkCatalogueIncompleteFormat, 1), gallery.Status);
            else
                Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, L.AtkCatalogueCompleteFormat, 1, 1), gallery.Status);
        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task GalleryShowsOnlyPureKeyframesAndStillScansPastHiddenPages(int sort)
    {
        var original=JsonNode.Parse(Response)!;
        var first=original.DeepClone();first["data"]!["total"]=40;
        var entries=first["data"]!["list"]!.AsArray();entries.Clear();
        foreach(var mode in new[]{"static","mixed","module","wave"})
        {
            var entry=original["data"]!["list"]![0]!.DeepClone();
            var info=entry["configurationBaseManagement"]!;info["configurationId"]=(600+entries.Count).ToString();
            var scene=info["scenePreset"]!;
            var effect=JsonNode.Parse("{\"type\":\"static\",\"config\":{}}")!;
            if(mode=="static")scene["effects"]=new JsonArray(effect);
            else if(mode=="mixed")scene["effects"]!.AsArray().Add(effect);
            else if(mode=="module") {scene["settings"]=JsonNode.Parse("{\"autoLayout\":true}");scene["animations"]=JsonNode.Parse("[{\"type\":\"AudioFrequency\",\"visible\":true}]");}
            else scene["effects"]![0]!["type"]="wave";
            entries.Add(entry);
        }
        var second=original.DeepClone();second["data"]!["total"]=40;
        int requests=0;
        using var http=new HttpClient(new Handler(async request=>
        {
            using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync());requests++;
            Assert.Equal(sort==1?"latest":"hot",body.RootElement.GetProperty("sort").GetString());
            return Reply((body.RootElement.GetProperty("current").GetInt32()==1?first:second).ToJsonString());
        }));
        string path=Path.GetTempFileName();await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var main=new MainViewModel(controller,new SettingsService(path),new NoStartup());main.RestoreSettings();
            using var gallery=new AtkGalleryViewModel(main,new AtkGalleryClient(http)){SortIndex=sort};await gallery.LoadAsync();
            Assert.Equal(2,requests);Assert.True(Assert.Single(gallery.Items).IsKeyframe);Assert.Equal("528",gallery.Items[0].Id);
            using var staticHttp=new HttpClient(new Handler(_=>Task.FromResult(Reply(first.ToJsonString()))));
            var staticPreset=Assert.Single((await new AtkGalleryClient(staticHttp).GetPageAsync("",false,1,TestContext.Current.CancellationToken)).Items,p=>p.Id=="600");
            Assert.True(staticPreset.IsSupported);gallery.SelectedItem=staticPreset;await gallery.PreparePreviewAsync();
            Assert.False(gallery.CanApply);Assert.False(await gallery.ApplyAsync());
        }
        finally{File.Delete(path);}
    }

    private sealed class QuietLog : ILogSink { public void Log(LogLevel level, string message, Exception? exception = null) { } }
    private sealed class NoStartup : IStartupManager
    {
        public bool IsSupported => false;
        public bool IsEnabled() => false;
        public void SetEnabled(bool enabled) { }
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
