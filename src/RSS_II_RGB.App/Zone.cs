using System.Globalization;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Effects.Layers;

namespace RSS_II_RGB.App;

/// <summary>How an Audio zone renders.</summary>
internal enum AudioZoneMode
{
    Spectrum,     // per-column frequency spectrum
    SolidColor,   // whole zone brightness = volume, fixed colour
    SolidRainbow, // whole zone brightness = volume, cycling rainbow
}

/// <summary>
/// A user-defined group of keys with its own effect — the Synapse-style override
/// layered on top of the global effect.
/// </summary>
internal sealed record Zone(
    IReadOnlyList<int> KeyIndices,
    EffectChoice Effect,
    Rgb Color,
    AudioZoneMode AudioMode = AudioZoneMode.Spectrum,
    EffectSetting? Parameters = null);

/// <summary>Conversions between the live <see cref="Zone"/> and its persisted form.</summary>
internal static class ZoneMapping
{
    public static Zone ToZone(ZoneSetting setting)
    {
        PresetCatalog.NormalizeZone(setting);
        return new(setting.KeyIndices, setting.Effect, ParseRgb(setting.ColorHex), setting.AudioMode,setting.Parameters);
    }

    public static ZoneSetting ToSetting(Zone zone) => new()
    {
        KeyIndices = zone.KeyIndices is int[] arr ? arr : zone.KeyIndices.ToArray(),
        Effect = zone.Effect,
        ColorHex = $"{zone.Color.R:X2}{zone.Color.G:X2}{zone.Color.B:X2}",
        AudioMode = zone.AudioMode,
        Parameters = zone.Parameters,
    };

    public static PatternOptions Options(Zone zone)
    {
        var p=zone.Parameters??new EffectSetting {Rainbow=false};
        return new PatternOptions {Speed=Safe(p.Speed,.1,5,1),Width=Safe(p.Width,.25,3,1),Density=Safe(p.Density,.05,1,.3),Direction=Safe(p.Direction,0,360,0),Saturation=Safe(p.Saturation,0,100,100)/100,Rainbow=p.Rainbow,MirrorX=p.MirrorX,MirrorY=p.MirrorY,WaveColor1=ParseRgb(p.WaveColor1Hex),WaveColor2=ParseRgb(p.WaveColor2Hex)};
    }
    public static double Brightness(Zone zone)=>Safe(zone.Parameters?.Brightness??100,0,100,100)/100;
    private static double Safe(double v,double min,double max,double fallback)=>double.IsFinite(v)?Math.Clamp(v,min,max):fallback;

    public static Rgb ParseRgb(string hex)
    {
        hex = hex.Trim().TrimStart('#');
        if (hex.Length == 6 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
        {
            return new Rgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }
        return Rgb.White;
    }
}
