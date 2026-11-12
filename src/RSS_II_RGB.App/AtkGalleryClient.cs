using System.Text.Json;
using System.IO;

namespace RSS_II_RGB.App;

internal sealed record AtkPreset(string Id, string Title, string Author, string Description, JsonElement Scene, string DeviceName = "")
{
    public bool IsKeyframe => Scene.ValueKind == JsonValueKind.Object
        && (!Scene.TryGetProperty("animations", out var modules) || modules.ValueKind == JsonValueKind.Array && modules.GetArrayLength() == 0)
        && Scene.TryGetProperty("effects", out var effects) && effects.ValueKind == JsonValueKind.Array && effects.GetArrayLength() > 0
        && effects.EnumerateArray().All(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String && t.GetString() == "keyframe");
    private bool? _supported;
    public bool IsSupported
    {
        get
        {
            if (_supported is { } supported) return supported;
            try { _ = RSS_II_RGB.Core.Animation.AtkScene.Parse(Scene, RSS_II_RGB.Core.Layout.Falchion75Layout.Profile, false); _supported = true; return true; }
            catch (Exception e) when (e is InvalidDataException or InvalidOperationException or KeyNotFoundException) { _supported = false; return false; }
        }
    }
    public string Compatibility => IsKeyframe ? L.AtkKeyframes : IsSupported ? L.AtkAudioSupported : L.AtkUnsupported;
}
internal sealed record AtkPage(IReadOnlyList<AtkPreset> Items, int Total, int ReturnedCount);

internal sealed class AtkGalleryClient(HttpClient? http = null)
{
    private static readonly HttpClient PublicHttp = new(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
    private readonly HttpClient _http = http ?? PublicHttp;

    public async Task<AtkPage> GetPageAsync(string keyword, bool latest, int page, CancellationToken ct = default)
    {
        if (page is < 1 or > 10000) throw new ArgumentOutOfRangeException(nameof(page));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        ct = deadline.Token;
        keyword = (keyword ?? "").Trim();
        if (keyword.Length > 64) keyword = keyword[..64];
        using var body = new MemoryStream();
        using (var writer = new Utf8JsonWriter(body))
        {
            writer.WriteStartObject();
            writer.WriteString("configType", "music_rhythm");
            writer.WriteString("type", "square");
            writer.WriteString("sort", latest ? "latest" : "hot");
            writer.WriteNumber("current", page);
            writer.WriteNumber("pageSize", AppConstants.AtkPageSize);
            if (keyword.Length > 0) writer.WriteString("keyword", keyword);
            writer.WriteEndObject();
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, AppConstants.AtkGalleryEndpoint) { Content = new ByteArrayContent(body.ToArray()) };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > AppConstants.AtkMaxResponseBytes) throw new InvalidDataException("Gallery response is too large.");
        await using var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var content = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            if (content.Length + count > AppConstants.AtkMaxResponseBytes) throw new InvalidDataException("Gallery response is too large.");
            await content.WriteAsync(buffer.AsMemory(0, count), ct).ConfigureAwait(false);
        }
        content.Position = 0;
        using var document = await JsonDocument.ParseAsync(content, new JsonDocumentOptions { MaxDepth = 32 }, ct).ConfigureAwait(false);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("statusCode", out var status) || !status.TryGetInt32(out int code) || code != 200)
            throw new InvalidDataException("Gallery request was rejected.");
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("list", out var list) || list.ValueKind != JsonValueKind.Array || list.GetArrayLength() > AppConstants.AtkPageSize
            || !data.TryGetProperty("total", out var total) || !total.TryGetInt32(out int totalCount) || totalCount < 0)
            throw new InvalidDataException("Invalid gallery response.");
        var items = new List<AtkPreset>();
        foreach (var entry in list.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("configurationBaseManagement", out var info) || info.ValueKind != JsonValueKind.Object) continue;
            string id = Text(info, "configurationId", 20), title = Text(info, "configurationName", 160);
            if (id.Length == 0 || id.Any(c => c is < '0' or > '9') || title.Length == 0 || !info.TryGetProperty("scenePreset", out var scene) || scene.ValueKind != JsonValueKind.Object) continue;
            string author = "", description = "";
            if (entry.TryGetProperty("cloudConfigurationSquare", out var square) && square.ValueKind == JsonValueKind.Object
                && square.TryGetProperty("configurationCardInfo", out var card) && card.ValueKind == JsonValueKind.Object) author = Text(card, "authorNickname", 100);
            if (entry.TryGetProperty("configurationSharing", out var sharing) && sharing.ValueKind == JsonValueKind.Object) description = Text(sharing, "configurationDescription", 1500);
            items.Add(new AtkPreset(id, title, author, description, scene.Clone(), Text(info, "deviceName", 160)));
        }
        return new AtkPage(items, totalCount, list.GetArrayLength());
    }

    private static string Text(JsonElement value, string name, int limit)
    {
        string text = value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString()!.Trim() : "";
        return text.Length > limit ? text[..limit] : text;
    }
}
