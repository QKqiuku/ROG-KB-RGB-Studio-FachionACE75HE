using RSS_II_RGB.Core.Engine;
using System.Text.Json.Serialization;

namespace RSS_II_RGB.App;

/// <summary>Persisted app state — restored on launch so a setup survives restarts.</summary>
internal sealed class AppSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter<EffectChoice>))]
    public EffectChoice GlobalEffect { get; set; } = EffectChoice.Rainbow;

    public string GlobalColorHex { get; set; } = "00FF66";

    public double BrightnessPercent { get; set; } = 100;
    public int? TargetFps { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter<DecorationMode>))]
    public DecorationMode LightBarMode { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter<DecorationMode>))]
    public DecorationMode EyeMode { get; set; }
    public OutputEnvelopeOptions OverallEffect {get;set;}=new();
    public DecorationSetting BarEffect {get;set;}=new();
    public DecorationSetting EyeEffect {get;set;}=new();
    public bool UseCustomLayers { get; set; } = true;
    public EffectChoice? CustomBaseEffect { get; set; }
    public Dictionary<EffectChoice, EffectSetting> EffectSettings { get; set; } = new();
    public List<UserLightingPreset> UserPresets {get;set;}=[];
    public string? ActiveUserPresetId {get;set;}
    public List<EffectChoice> FavoriteEffects { get; set; } = new();
    public List<EffectChoice> RecentEffects { get; set; } = new();

    // Independent display overlays, layered on top of the base effect.
    public bool EnableReactive { get; set; }
    public EffectChoice ReactiveEffect { get; set; } = EffectChoice.Reactive;

    public bool EnableAudio { get; set; }
    public RSS_II_RGB.Core.Audio.AudioAnalysisOptions MusicAudioOptions { get; set; } = new();
    public string? MusicSourcePath { get; set; }
    public string? MusicSourceName { get; set; }

    // Starlight overlay (twinkling stars above the base effect, below other overlays).
    public bool EnableStarlight { get; set; }

    public double AudioSensitivity { get; set; } = 0.9;

    // Global audio overlay: spectrum vs three-region bars, and the per-region bars multipliers.
    [JsonConverter(typeof(JsonStringEnumConverter<AudioLayoutChoice>))]
    public AudioLayoutChoice AudioLayout { get; set; } = AudioLayoutChoice.Spectrum;

    public double AudioBarsBassMultiplier { get; set; } = 0.9;

    public double AudioBarsMidMultiplier { get; set; } = 1.0;

    public double AudioBarsTrebleMultiplier { get; set; } = 2.0;

    // System-metric overlay.
    public bool ShowMetrics { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<MetricLayoutChoice>))]
    public MetricLayoutChoice MetricLayout { get; set; } = MetricLayoutChoice.FunctionRow;

    public double[] PercentThresholds { get; set; } = { 30, 60, 90 };

    public double[] TempThresholds { get; set; } = { 55, 65, 75 };

    // GIF animation effect: the baked .kbanim plus the source GIF + crop rectangle
    // (source pixels: [x, y, width, height]) so the import can be re-opened/re-edited.
    public string? GifAnimPath { get; set; }

    public string? GifSourcePath { get; set; }
    public string? AtkAnimPath { get; set; }
    public string? AtkPresetName { get; set; }
    public string? AtkPresetAuthor { get; set; }
    public string? AtkPresetId { get; set; }
    public bool AtkFitLayout { get; set; }

    public int[] GifCrop { get; set; } = Array.Empty<int>();

    public List<ZoneSetting> Zones { get; set; } = new();
}

internal sealed class EffectSetting
{
    public bool MirrorX { get; set; }
    public bool MirrorY { get; set; }
    public double Speed { get; set; } = 1;
    public double Width { get; set; } = 1;
    public double Density { get; set; } = 0.3;
    public double Direction { get; set; }
    public double Saturation { get; set; } = 100;
    public bool Rainbow { get; set; } = true;
    public bool ReactiveRainbow { get; set; }
    public string ColorHex { get; set; } = "00FF66";
    public string WaveColor1Hex { get; set; } = "00D1FF";
    public string WaveColor2Hex { get; set; } = "FF0099";
    public double Brightness { get; set; } = 100;
}

/// <summary>Serializable form of a <see cref="Zone"/>.</summary>
internal sealed class ZoneSetting
{
    public int[] KeyIndices { get; set; } = Array.Empty<int>();
    public EffectSetting? Parameters {get;set;}

    [JsonConverter(typeof(JsonStringEnumConverter<EffectChoice>))]
    public EffectChoice Effect { get; set; } = EffectChoice.Solid;

    public string ColorHex { get; set; } = "FFFFFF";

    [JsonConverter(typeof(JsonStringEnumConverter<AudioZoneMode>))]
    public AudioZoneMode AudioMode { get; set; } = AudioZoneMode.Spectrum;
}

// Source-generated, reflection-free JSON (CLAUDE.md rule 5).
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(LightingComposition))]
internal partial class AppSettingsJsonContext : JsonSerializerContext
{
}

internal sealed class DecorationSetting
{
    public int EffectIndex {get;set;}
    public bool MusicRainbow {get;set;}
    public string ColorHex {get;set;}="FF003C";
    public double Speed {get;set;}=1;
    public double Brightness {get;set;}=100;
}
