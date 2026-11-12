using RSS_II_RGB.Core.Layout;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RSS_II_RGB.App;

internal static partial class AtkLayoutFilter
{
    public static bool Matches(AtkPreset preset, KeyboardProfile profile)
    {
        if (profile.FormFactor == KeyboardFormFactor.Unknown) return false;
        var declared = Detect(preset.DeviceName + " " + preset.Title);
        if (declared.Count > 1 || declared.Count == 1 && !declared.Contains(profile.FormFactor)) return false;
        bool confirmed = declared.Contains(profile.FormFactor);
        if (preset.Scene.ValueKind != JsonValueKind.Object) return false;
        if (preset.Scene.TryGetProperty("effects", out var effects))
        {
            if (effects.ValueKind != JsonValueKind.Array) return false;
            foreach (var effect in effects.EnumerateArray())
            {
                if (effect.ValueKind != JsonValueKind.Object || !effect.TryGetProperty("config", out var config) || config.ValueKind != JsonValueKind.Object) return false;
                if (config.TryGetProperty("enabled", out var enabled) && enabled.ValueKind == JsonValueKind.False) continue;
                if (!config.TryGetProperty("customParams", out var parameters) || parameters.ValueKind != JsonValueKind.Object) continue;
                bool hasRows = parameters.TryGetProperty("sourceRows", out var rows), hasCols = parameters.TryGetProperty("sourceCols", out var cols);
                if (!hasRows && !hasCols) continue;
                if (!hasRows || !hasCols || rows.ValueKind != JsonValueKind.Number || cols.ValueKind != JsonValueKind.Number
                    || !rows.TryGetInt32(out int r) || !cols.TryGetInt32(out int c)) return false;
                var source = (r, c) switch
                {
                    (5, 14) => KeyboardFormFactor.Compact60,
                    (5, 15) => KeyboardFormFactor.Compact65,
                    (6, 15) => KeyboardFormFactor.Compact75,
                    (6, 17) => KeyboardFormFactor.Tenkeyless,
                    _ when r == profile.Rows && c == profile.Cols => profile.FormFactor,
                    _ => KeyboardFormFactor.Unknown,
                };
                if (source != profile.FormFactor) return false;
                confirmed = true;
            }
        }
        // Coloured frame bounds are sparse artwork, not source keyboard dimensions.
        if (confirmed) return true;
        var description = Detect(preset.Description);
        return description.Count == 1 && description.Contains(profile.FormFactor);
    }

    private static HashSet<KeyboardFormFactor> Detect(string text)
    {
        var result = new HashSet<KeyboardFormFactor>();
        if (SeventyFiveModels().IsMatch(text)) result.Add(KeyboardFormFactor.Compact75);
        if (SixtyFiveModels().IsMatch(text)) result.Add(KeyboardFormFactor.Compact65);
        if (SixtyModels().IsMatch(text)) result.Add(KeyboardFormFactor.Compact60);
        if (TklModels().IsMatch(text)) result.Add(KeyboardFormFactor.Tenkeyless);
        if (NinetySixModels().IsMatch(text)) result.Add(KeyboardFormFactor.Compact96);
        if (FullSizeKeys().IsMatch(text)) result.Add(KeyboardFormFactor.FullSize);
        foreach (Match match in Percentages().Matches(text)) result.Add(match.Groups[1].Value switch
        {
            "60" => KeyboardFormFactor.Compact60, "65" => KeyboardFormFactor.Compact65,
            "75" => KeyboardFormFactor.Compact75, "80" => KeyboardFormFactor.Tenkeyless,
            "96" => KeyboardFormFactor.Compact96, _ => KeyboardFormFactor.FullSize,
        });
        return result;
    }

    [GeneratedRegex(@"(?<![A-Z0-9])(?:ATK[\s_-]*)?(?:RS[\s_-]*7(?!\d)|EDGE[\s_-]*75(?!\d)|YOGO[\s_-]*75(?!\d)|V75[\s_-]*[XK](?!\d))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SeventyFiveModels();
    [GeneratedRegex(@"(?<![A-Z0-9])(?:ATK[\s_-]*)?(?:RS[\s_-]*6(?!\d)|68[\s_-]*(?:RX|HE|V\d+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SixtyFiveModels();
    [GeneratedRegex(@"(?<![A-Z0-9])(?:ATK[\s_-]*)?(?:RS[\s_-]*63(?!\d)|EDGE[\s_-]*(?:60|63)(?!\d)|60[\s_-]*RX)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SixtyModels();
    [GeneratedRegex(@"(?<![A-Z0-9])(?:ATK[\s_-]*)?(?:RS[\s_-]*8(?!\d)|A87(?!\d)|Z87(?!\d)|HEX[\s_-]*80(?!\d))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TklModels();
    [GeneratedRegex(@"(?<![A-Z0-9])(?:ATK[\s_-]*)?A98(?!\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NinetySixModels();
    [GeneratedRegex(@"(?<!\d)(?:104|108)\s*[- ]?\s*(?:键|keys?\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FullSizeKeys();
    [GeneratedRegex(@"(?<!\d)(60|65|75|80|96|100)\s*(?:%\s*)?(?:配列|布局|layout\b|keyboard\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Percentages();
}
