using System.Text.Json;
using RSS_II_RGB.Core.Audio;
using System.Globalization;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Animation;

public sealed partial class AtkScene
{
    private sealed record Layer(Rgb?[][] Frames, int[] Ends, double Speed, double Brightness, double Opacity, bool Loop, AudioLayer? Audio = null);
    private readonly Layer[] _layers;
    private readonly int _ledCount;
    public bool UsesAudio => _layers.Any(l => l.Audio?.UsesAudio == true);
    // Rebuilt layers share the same playback origin across frame-rate and zone edits.
    internal TimeSpan? PlaybackStart;
    private readonly KeyboardProfile _profile;
    private AtkScene(Layer[] layers, KeyboardProfile profile) { _layers = layers; _profile = profile; _ledCount = profile.LedCount; }
    public int FrameCount => _layers.Sum(l => l.Frames.Length);

    public static AtkScene Parse(JsonElement scene, KeyboardProfile profile, bool fit)
    {
        if (scene.ValueKind != JsonValueKind.Object || Number(scene, "version", 0, 1, 1) != 1)
            throw new InvalidDataException("Unsupported scene version.");
        if (!scene.TryGetProperty("effects", out var effects) || effects.ValueKind != JsonValueKind.Array || effects.GetArrayLength() > 8)
            throw new InvalidDataException("Invalid effect list.");
        var layers = new List<Layer>();
        ParseModules(scene, profile, layers);
        int totalFrames = 0;
        foreach (var effect in effects.EnumerateArray())
        {
            if (effect.ValueKind != JsonValueKind.Object || !effect.TryGetProperty("config", out var config) || config.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Missing effect configuration.");
            if (!Flag(config, "enabled", true)) continue;
            if (!effect.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("Missing effect type.");
            if (type.GetString() != "keyframe")
            {
                layers.Add(Procedural(type.GetString()!, config, profile));
                continue;
            }
            if (!config.TryGetProperty("customParams", out var parameters) || parameters.ValueKind != JsonValueKind.Object
                || !parameters.TryGetProperty("frames", out var source) || source.ValueKind != JsonValueKind.Array || source.GetArrayLength() < 1)
                throw new InvalidDataException("Missing animation frames.");
            totalFrames += source.GetArrayLength();
            if (totalFrames > 4000 || (long)totalFrames * profile.LedCount > 1_000_000)
                throw new InvalidDataException("Scene exceeds the decoded frame budget.");

            var sourceFrames = source.EnumerateArray().ToArray();
            var cells = new List<Dictionary<(int Row, int Col), Rgb>>();
            int inferredRows = 0, inferredCols = 0;
            foreach (var frame in sourceFrames)
            {
                if (frame.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Invalid frame.");
                var map = new Dictionary<(int Row, int Col), Rgb>();
                foreach (var cell in frame.EnumerateObject())
                {
                    var parts = cell.Name.Split('_');
                    if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int row)
                        || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int col)
                        || row >= 16 || col >= 32 || cell.Value.ValueKind != JsonValueKind.String)
                        throw new InvalidDataException("Invalid source coordinate.");
                    string hex = cell.Value.GetString()!;
                    if (hex.Length != 7 || hex[0] != '#' || !uint.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
                        throw new InvalidDataException("Invalid RGB colour.");
                    if (!map.TryAdd((row, col), new Rgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb)))
                        throw new InvalidDataException("Duplicate source cell.");
                    inferredRows = Math.Max(inferredRows, row + 1);
                    inferredCols = Math.Max(inferredCols, col + 1);
                }
                cells.Add(map);
            }
            int rows = Dimension(parameters, "sourceRows", Math.Max(1, inferredRows), 16);
            int cols = Dimension(parameters, "sourceCols", Math.Max(1, inferredCols), 32);
            if (rows < inferredRows || cols < inferredCols) throw new InvalidDataException("Source dimensions exclude coloured cells.");
            var frames = new Rgb?[sourceFrames.Length][];
            for (int f = 0; f < frames.Length; f++)
            {
                bool hasDecorations = ReferenceEquals(profile, Falchion75Layout.Profile);
                frames[f] = new Rgb?[profile.LedCount + (hasDecorations ? Falchion75Layout.LightBarLedCount : 0)];
                foreach (var key in profile.Keys)
                {
                    int row = fit ? (key.Row * 2 + 1) * rows / (profile.Rows * 2) : key.Row;
                    int col = fit ? (key.Col * 2 + 1) * cols / (profile.Cols * 2) : key.Col;
                    bool falchion75 = ReferenceEquals(profile, Falchion75Layout.Profile) && rows == 6 && cols == 15;
                    if (falchion75)
                    {
                        // RS7 animation anchors differ from the ROG effect matrix; never change HID addresses here.
                        if (key.Name == "ANSI_ENTER") col = 13;
                        else if (key.Row == 4 && key.Col is >= 2 and <= 11) col = key.Col - 1;
                        else if (key.Name == "RIGHT_SHIFT") col = 11;
                        else if (key.Name == "SPACE") col = 5;
                    }
                    if (cells[f].TryGetValue((row, col), out var color)) frames[f][key.Index] = color;
                    // shortcut: RS7 has no matching LED at these ROG positions; borrow an adjacent cell only when absent.
                    else if (falchion75 && key.Name == "PAGE_DOWN" && cells[f].TryGetValue((3, 14), out color)) frames[f][key.Index] = color;
                    else if (falchion75 && key.Name == "RIGHT_CONTROL" && cells[f].TryGetValue((5, 12), out color)) frames[f][key.Index] = color;
                }
                if (hasDecorations)
                {
                    for(int i=0;i<Falchion75Layout.LightBarLedCount;i++)
                    {
                        int col = Falchion75Layout.LightBarSourceIndices[i];
                        if (fit) col = (col * 2 + 1) * cols / (profile.Cols * 2);
                        if (cells[f].TryGetValue((0,col),out var color)) frames[f][profile.LedCount+i] = color;
                    }
                }
            }
            var ends = new int[frames.Length];
            bool hasDelays = parameters.TryGetProperty("frameDelays", out var delays);
            if (hasDelays && (delays.ValueKind != JsonValueKind.Array || delays.GetArrayLength() != frames.Length))
                throw new InvalidDataException("Frame delays must match frame count.");
            double defaultDelay = Number(parameters, "frameDuration", 200, 1, 60000);
            int cumulative = 0;
            for (int f = 0; f < frames.Length; f++)
            {
                double delay = hasDelays && delays[f].ValueKind == JsonValueKind.Number && delays[f].TryGetDouble(out double d) ? d : hasDelays ? double.NaN : defaultDelay;
                if (!double.IsFinite(delay) || delay < 1 || delay > 60000) throw new InvalidDataException("Invalid frame delay.");
                cumulative += (int)Math.Round(delay, MidpointRounding.AwayFromZero);
                ends[f] = cumulative;
            }
            layers.Add(new Layer(frames, ends, Number(config, "speed", 1, 0.1, 5), Number(config, "brightness", 100, 0, 100) / 100,
                Number(config, "opacity", 1, 0, 1), Flag(parameters, "loop", true)));
        }
        if (layers.Count == 0) throw new InvalidDataException("No enabled supported layers.");
        return new AtkScene(layers.ToArray(), profile);
    }

    public void Render(Span<Rgb> target, TimeSpan elapsed, AudioFrame? audio = null)
    {
        if (target.Length != _ledCount && target.Length != _ledCount + (ReferenceEquals(_profile, Falchion75Layout.Profile) ? Falchion75Layout.LightBarLedCount : 0)) throw new ArgumentException("Unexpected target size.", nameof(target));
        target.Clear();
        foreach (var layer in _layers)
        {
            if (layer.Opacity <= 0) continue;
            if (layer.Audio is { } procedural)
            {
                procedural.Render(target, elapsed, audio, layer.Brightness, layer.Opacity);
                continue;
            }
            double time = Math.Max(0, elapsed.TotalMilliseconds) * layer.Speed;
            time = layer.Loop ? time % layer.Ends[^1] : Math.Min(time, layer.Ends[^1] - 1);
            int f = Array.BinarySearch(layer.Ends, (int)Math.Floor(time));
            f = f >= 0 ? f + 1 : ~f;
            var pixels = layer.Frames[Math.Min(f, layer.Frames.Length - 1)];
            for (int i = 0; i < target.Length; i++)
            {
                if (pixels[i] is not { } color) continue;
                var previous = target[i];
                target[i] = new Rgb(Blend(color.R, previous.R), Blend(color.G, previous.G), Blend(color.B, previous.B));
                // ATK shared scenes use screen mixing; keyframe opacity only gates visibility.
                byte Blend(byte channel, byte background) => (byte)Math.Clamp(Math.Round(255 -
                    (255 - Math.Round(channel * layer.Brightness, MidpointRounding.AwayFromZero)) * (255 - background) / 255,
                    MidpointRounding.AwayFromZero), 0, 255);
            }
        }
    }

    private static double Number(JsonElement obj, string name, double fallback, double min, double max)
    {
        if (!obj.TryGetProperty(name, out var value)) return fallback;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number) || !double.IsFinite(number) || number < min || number > max)
            throw new InvalidDataException($"Invalid {name}.");
        return number;
    }
    private static bool Flag(JsonElement obj, string name, bool fallback)
    {
        if (!obj.TryGetProperty(name, out var value)) return fallback;
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False) throw new InvalidDataException($"Invalid {name}.");
        return value.GetBoolean();
    }
    private static int Dimension(JsonElement obj, string name, int fallback, int max)
    {
        if (!obj.TryGetProperty(name, out var value)) return fallback;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int number) || number < 1 || number > max)
            throw new InvalidDataException($"Invalid {name}.");
        return number;
    }
}
