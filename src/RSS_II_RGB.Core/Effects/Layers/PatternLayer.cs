using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Effects.Layers;

public enum PatternKind
{
    Solid, Breathing, Rainbow, PressFade, Rain, Ripple, Stars, Wave,
    SineWave, Waterfall, Bloom, BrickBreaker, CountdownFireworks, Vortex, Aurora, Ocean, Nebula, Fire, Candle, Meteor, Radar, Heartbeat, Checker, Snake, ScanBand, Wipe, Marble, Silk, Dna, Sunset, RainDrops, Lava, Lightning, NeonPulse,
    Marquee, LakePixels, LakeRipples, LakeSurge, PerimeterFlow, RotatingRing, DoubleBeacon, SineSectors, WindmillCycle, Caustics, Sun, Sakura, IceCrystal, AuroraRipples,
}

public sealed record PatternOptions
{
    public bool MirrorX { get; init; }
    public bool MirrorY { get; init; }
    public double Speed { get; init; } = 1;
    public double Width { get; init; } = 1;
    public double Density { get; init; } = 0.3;
    public double Direction { get; init; }
    public double Saturation { get; init; } = 1;
    public bool Rainbow { get; init; } = true;
    public Rgb? WaveColor1 { get; init; }
    public Rgb? WaveColor2 { get; init; }
    public bool ReactiveRainbow { get; init; }
}

/// <summary>Time-based software presets; geometry is normalized to the active keyboard.</summary>
public sealed partial class PatternLayer : IEffectLayer
{
    private const double Tau = 2 * Math.PI;
    private static readonly string[] CountdownDigits =
        ["111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
         "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111"];
    private readonly PatternKind _kind;
    private readonly Rgb _color;
    private readonly PatternOptions _options;
    private readonly List<KeyHit> _hits = new();
    private double? _animationStart;

    public PatternLayer(string id, PatternKind kind, Rgb color, KeyMask mask, PatternOptions options, int zOrder = 0)
    {
        Id = id;
        ZOrder = zOrder;
        Mask = mask;
        _kind = kind;
        _color = color;
        _options = options with
        {
            Speed = Safe(options.Speed, 0.1, 5, 1), Width = Safe(options.Width, 0.25, 3, 1),
            Density = Safe(options.Density, 0.05, 1, 0.3), Direction = Safe(options.Direction, 0, 360, 0),
            Saturation = Safe(options.Saturation, 0, 1, 1)
        };
    }

    public string Id { get; }
    public int ZOrder { get; }
    public BlendMode Blend => BlendMode.Normal;
    public KeyMask Mask { get; }
    public bool IsComplete => false;

    public void Render(Span<Rgb> target, in EffectContext ctx)
    {
        bool reactive = _kind is PatternKind.PressFade or PatternKind.Ripple;
        double seconds = ctx.Elapsed.TotalSeconds;
        if (reactive)
        {
            foreach (var hit in ctx.NewHits)
                if ((uint)hit.KeyIndex < (uint)target.Length && Mask.Contains(hit.KeyIndex)) _hits.Add(hit);
            _hits.RemoveAll(h => (seconds - h.When.TotalSeconds) * _options.Speed > 2);
            if (_hits.Count > 256) _hits.RemoveRange(0, _hits.Count - 256);
        }

        double t = seconds * _options.Speed;
        if (_kind == PatternKind.CountdownFireworks)
        {
            _animationStart ??= seconds;
            t = Math.Max(0, seconds - _animationStart.Value) * _options.Speed;
        }
        double angle = _options.Direction / 360 * Tau;
        double cos = Math.Cos(angle), sin = Math.Sin(angle);
        for (int i = 0; i < target.Length; i++)
        {
            var key = ctx.Layout.ByIndex(i);
            double x = (double)key.Col / Math.Max(1, ctx.Layout.Cols - 1);
            double y = (double)key.Row / Math.Max(1, ctx.Layout.Rows - 1);
            if (_options.MirrorX) x = 1 - x;
            if (_options.MirrorY) y = 1 - y;
            double u = (x - 0.5) * cos + (y - 0.5) * sin + 0.5;
            double v = -(x - 0.5) * sin + (y - 0.5) * cos + 0.5;
            target[i] = reactive ? ReactivePixel(i, seconds, ctx) : Pixel(i, key.Row, key.Col, u, v, u, v, t, ctx);
        }
    }

    private Rgb Pixel(int index, int row, int col, double x, double y, double u, double v, double t, in EffectContext ctx)
    {
        if (TryLumoPixel(u, v, t, ctx, out var lumoPixel)) return lumoPixel;
        double width = _options.Width, density = _options.Density;
        double dx = x - 0.5, dy = y - 0.5;
        double radius = Math.Sqrt(dx * dx + dy * dy);
        double polar = Math.Atan2(dy, dx) / Tau;
        double hue = u / width + t * 0.18;
        double light = 1, saturation = 1;
        switch (_kind)
        {
            case PatternKind.Solid: return _color;
            case PatternKind.Breathing:
                light = 0.5 + 0.5 * Math.Sin(t * Tau / 3);
                break;
            case PatternKind.Rainbow:
                hue = (double)index / ctx.Layout.LedCount / width + t * 0.2;
                break;
            case PatternKind.Wave:
                break;
            case PatternKind.Rain:
                double seed = Hash(col + Math.Floor(t * 0.2) * 37);
                double drop = Frac(t * 0.45 + Hash(col * 13 + 1));
                double behind = Frac(drop - v);
                light = seed < density ? Math.Max(0, 1 - behind / (0.08 + 0.2 * width)) : 0;
                hue = Hash(col * 7) + t * 0.1;
                break;
            case PatternKind.Stars:
                double phase = Frac(t * 0.25 + Hash(index * 17 + 3));
                light = phase < density ? Math.Pow(Math.Sin(Math.PI * phase / density), 2) : 0;
                hue = Hash(index * 19) + Math.Floor(t * 0.25) * 0.1;
                break;
            case PatternKind.Waterfall:
                hue = v / width - t * 0.3 + Hash(col * 11) * density;
                light = 0.25 + 0.75 * Frac(t * 0.7 - v + Hash(col * 5));
                break;
            case PatternKind.Bloom:
                double ring = Frac(t * 0.23) * 0.85;
                double petals = 0.06 * Math.Cos(polar * Tau * (3 + Math.Round(density * 8)));
                light = Math.Max(0, 1 - Math.Abs(radius - ring - petals) / (0.07 + width * 0.09));
                hue = polar + t * 0.15;
                break;
            case PatternKind.BrickBreaker:
                double ballX = Triangle(t * 0.18), ballY = 0.25 + 0.7 * Triangle(t * 0.29);
                double ball = Math.Max(0, 1 - Math.Sqrt(Math.Pow((x - ballX) * 14, 2) + Math.Pow((y - ballY) * 5, 2)) / width);
                double paddle = y > 0.9 && Math.Abs(x - ballX) < 0.08 + width * 0.04 ? 1 : 0;
                double brick = row < 2 && Hash(index + Math.Floor(t / 8) * 83) > Frac(t / 8) * 0.8 ? 0.65 : 0;
                light = Math.Max(ball, Math.Max(paddle, brick));
                hue = x + row * 0.25;
                break;
            case PatternKind.CountdownFireworks:
                double cycle = t % 26;
                if (cycle < 10)
                {
                    int number = 10 - (int)cycle, digits = number == 10 ? 2 : 1;
                    int digitCol = col - (ctx.Layout.Cols - (digits * 4 - 1)) / 2;
                    int which = digitCol / 4, dc = digitCol % 4;
                    int digit = digits == 2 ? (which == 0 ? 1 : 0) : number;
                    bool on = row >= 0 && row < 5 && digitCol >= 0 && which < digits && dc < 3 && CountdownDigits[digit][row * 3 + dc] == '1';
                    return on ? Colour(.53, 1) : Rgb.Black;
                }
                double showTime = cycle - 10;
                light = 0;
                for (int burst = Math.Max(0, (int)(showTime / 1.4) - 1); burst <= (int)(showTime / 1.4); burst++)
                {
                    double age = showTime - burst * 1.4;
                    double ox = .2 + .6 * Hash(burst * 19 + 3), oy = .25 + .3 * Hash(burst * 23 + 2);
                    double glow = FireworkBurst.Light(x, y, ox, oy, age, width, density, burst);
                    if (glow > light) { light = glow; hue = Hash(burst * 11 + 5) + polar * .12; }
                }
                break;
            case PatternKind.Aurora:
                double greenCurtain = .35 + .24 * Math.Sin(v * 2.8 + t * .65) + .06 * Math.Sin(v * 9 - t);
                double purpleCurtain = .7 + .2 * Math.Sin(v * 3.2 - t * .55 + 2);
                double greenGlow = Math.Exp(-Math.Pow((u - greenCurtain) / (.11 * width), 2));
                double purpleGlow = Math.Exp(-Math.Pow((u - purpleCurtain) / (.13 * width), 2));
                light = (.25 + .75 * (1 - v)) * Math.Max(greenGlow, purpleGlow);
                hue = .36 + .42 * purpleGlow / Math.Max(.001, greenGlow + purpleGlow);
                saturation = .82; break;
            case PatternKind.Ocean:
                double seaPhase = v * 8 / width + Math.Sin(u * 5 - t * .7) * 1.8 - t * 1.5;
                double swell = .5 + .5 * Math.Sin(seaPhase);
                double undertow = .5 + .5 * Math.Sin(u * 4 + v * 5 / width - t * .9);
                double seaDepth = .8 * swell + .2 * undertow;
                hue = .52 + .1 * seaDepth; saturation = .9 - .12 * Math.Pow(seaDepth, 4);
                light = .22 + .78 * seaDepth; break;
            case PatternKind.Nebula:
                double cloud = (Math.Sin(u * 6 / width + Math.Sin(v * 5 + t * .5)) + Math.Sin(v * 5 - u * 3 + t * .7)) * .25 + .5;
                hue = .55 + .43 * cloud; saturation = .65 + .35 * Math.Sin(cloud * Math.PI);
                light = _options.Rainbow ? 1 : .25 + .75 * cloud; break;
            case PatternKind.Fire:
                double tongue = .5 + .5 * Math.Sin(u * 17 / width + v * 7 - t * 4 + .8 * Math.Sin(u * 7 + v * 5 - t * 2));
                double fuel = .5 + .48 * Math.Pow(.5 + .5 * Math.Sin(u * 21 / width + t * 3.4 + Math.Sin(t * 1.7)), 2);
                // A moving ember floor keeps every row alive while tongues carry the stronger heat.
                double fireHeat = Math.Clamp((.12 + .68 * Math.Clamp(v, 0, 1) + .2 * tongue * tongue) * fuel, 0, 1);
                hue = .005 + .12 * Math.Pow(fireHeat, 4); light = Math.Pow(fireHeat, 1.35); break;
            case PatternKind.Candle:
                hue = .062; saturation = .93;
                double flickerPhase = t * 9, flickerMix = Frac(flickerPhase);
                flickerMix *= flickerMix * (3 - 2 * flickerMix);
                light = .25 + .57 * (Hash(index * 13 + Math.Floor(flickerPhase)) * (1 - flickerMix) + Hash(index * 13 + Math.Floor(flickerPhase) + 1) * flickerMix);
                light += .12 * Math.Sin(t * 4.7); break;
            case PatternKind.Meteor:
                light = 0; saturation = .4;
                for (int m = 0; m < 5; m++)
                {
                    double head = Frac(t * (.22 + .08 * Hash(m * 7)) + Hash(m * 11 + 2));
                    double lane = Math.Round(Hash(m * 17 + 4) * (ctx.Layout.Rows - 1)) / Math.Max(1, ctx.Layout.Rows - 1);
                    double tail = Frac(head - u);
                    double glow = Math.Max(0, 1 - tail / (.23 * width)) * Math.Exp(-Math.Pow((v - lane) * 14 / width, 2));
                    if (glow > light) { light = glow; hue = Hash(m * 19 + 6); }
                }
                break;
            case PatternKind.Radar:
                light = Math.Pow(Math.Max(0, 1 - Frac(polar - t * .25)), 12 / width) * (1-radius); hue = .3 + .15 * radius; break;
            case PatternKind.Heartbeat:
                double beat = t % 1.12;
                light = .95 * Math.Exp(-Math.Pow((beat - .33) / .048, 2)) + .52 * Math.Exp(-Math.Pow((beat - .55) / .055, 2));
                if (light < 1.0 / 255) light = 0;
                hue = .984; break;
            case PatternKind.Checker:
                int square = (int)Math.Floor(u * (ctx.Layout.Cols - 1) / width) + (int)Math.Floor(v * (ctx.Layout.Rows - 1) / width);
                double checkerFade = .5 + .5 * Math.Sin(t * 2.8);
                light = .08 + .82 * (square % 2 == 0 ? checkerFade : 1 - checkerFade);
                hue = .66 + t * .19; saturation = .9; break;
            case PatternKind.Snake:
                double snakePath = (row + (row%2==0?x:1-x))/ctx.Layout.Rows;
                light = Math.Max(0,1-Frac(t*.12-snakePath)/(.18*width)); hue = snakePath * 2 - t*.1; break;
            case PatternKind.ScanBand:
                light = Math.Exp(-Math.Pow((u - Triangle(t * .18)) * 16 / width, 2));
                hue = .54 + .12 * Math.Sin(t * .8); saturation = .8; break;
            case PatternKind.Wipe:
                double wipeTime = t * .42 + .3;
                double edge = Frac(wipeTime) * 1.3 - .15;
                Rgb oldColour = Colour(Frac(Math.Floor(wipeTime) * .271 + .73), .85);
                Rgb newColour = Colour(Frac((Math.Floor(wipeTime) + 1) * .271 + .73), .85);
                double mix = Math.Clamp((edge - u) * 22 / width + .5, 0, 1);
                Rgb wiped = Rgb.Lerp(oldColour, newColour, (float)mix);
                return Rgb.Lerp(wiped, _options.Rainbow ? Rgb.White : _color, (float)(.55 * Math.Exp(-Math.Pow((u - edge) * 32 / width, 2))));
            case PatternKind.Marble:
                double vein = .5 + .5 * Math.Sin(u * 10 / width + v * 4 + 2 * Math.Sin(v * 5 - t * .9) - t * 1.4);
                hue = .58 + .2 * vein; saturation = .1 + .55 * vein;
                light = .97 - .72 * Math.Pow(vein, 5); break;
            case PatternKind.Silk:
                double fold = .5 + .5 * Math.Sin(u * 6 / width + v * 4 + 1.8 * Math.Sin(v * 4 - t * .65));
                hue = .94 + .16 * fold; saturation = .25 + .44 * fold; light = _options.Rainbow ? 1 : .3 + .7 * fold; break;
            case PatternKind.Dna:
                double dnaPhase = u * Tau * 2 / width - t * 2;
                double strand = Math.Sin(dnaPhase) * .38;
                double strandA = Math.Exp(-Math.Pow((dy - strand) * 8 / width, 2));
                double strandB = Math.Exp(-Math.Pow((dy + strand) * 8 / width, 2));
                light = Math.Max(strandA, strandB);
                hue = strandA > strandB ? .53 + .08 * Math.Sin(dnaPhase) : .93 + .06 * Math.Cos(dnaPhase);
                saturation = .95; break;
            case PatternKind.Sunset:
                double dusk = .5 + .5 * Math.Sin(v * 4 + t * .45 + .3 * Math.Sin(u * 3));
                hue = .77 + .36 * dusk; saturation = .55 + .35 * Math.Sin(dusk * Math.PI);
                light = .55 + .45 * dusk; break;
            case PatternKind.RainDrops:
                light = 0; hue = .58; saturation = .8;
                for (int lane = 0; lane < ctx.Layout.Cols; lane++)
                {
                    if (Hash(lane * 19 + 7) > .3 + density * .7) continue;
                    double dropX = (double)lane / Math.Max(1, ctx.Layout.Cols - 1);
                    double duration = 1.1 + Hash(lane * 11 + 2) * .6;
                    double dropAge = Frac(t / duration + Hash(lane * 17 + 4));
                    double headY = dropAge * 1.65 - .3;
                    double tail = headY - v;
                    double falling = Math.Exp(-Math.Pow((u - dropX) * (ctx.Layout.Cols - 1) * 2 / width, 2))
                        * (tail >= -.06 && tail < .32 * width ? Math.Pow(Math.Clamp(1 - Math.Max(0, tail) / (.32 * width), 0, 1), 1.5) : 0);
                    double splash = 0;
                    if (headY > 1)
                    {
                        double splashAge = (headY - 1) / .35;
                        double sx = Math.Abs(u - dropX) * (ctx.Layout.Cols - 1);
                        double sy = (1 - v) * (ctx.Layout.Rows - 1);
                        splash = Math.Exp(-Math.Pow((sx - splashAge * 1.6) * 2 / width, 2)
                            - Math.Pow((sy - Math.Sin(splashAge * Math.PI) * .6) * 3, 2)) * (1 - splashAge);
                    }
                    light = Math.Max(light, Math.Max(falling, splash));
                }
                break;
            case PatternKind.Lava:
                double heat = 0;
                for (int l = 0; l < 5; l++)
                {
                    double lx = .5 + .46 * Math.Sin(t * (.22 + l * .03) + Hash(l * 7) * Tau);
                    double ly = .5 + .45 * Math.Sin(t * .3 + Hash(l * 13 + 1) * Tau);
                    heat += Math.Exp(-(Math.Pow((u - lx) * 6 / width, 2) + Math.Pow((v - ly) * 3 / width, 2)));
                }
                heat = Math.Clamp(heat, 0, 1);
                hue = .005 + .065 * heat; light = .28 + .72 * heat; break;
            case PatternKind.Lightning:
                double stormAge = Frac(t * .6);
                double flash = Math.Exp(-Math.Pow((stormAge - .14) / .025, 2)) + .65 * Math.Exp(-Math.Pow((stormAge - .21) / .018, 2));
                double cloudShade = .35 + .65 * (.5 + .5 * Math.Sin(u * 6 + v * 4 + t * .7));
                light = .025 + flash * (.5 + .5 * cloudShade); hue = .633; saturation = .35; break;
            case PatternKind.NeonPulse:
                double neonWave = .5 + .5 * Math.Sin(radius * 22 / width - t * 3.4);
                hue = .51 + .22 * neonWave; saturation = .72 + .28 * neonWave; light = _options.Rainbow ? .95 : .2 + .75 * neonWave; break;
            case PatternKind.Vortex:
                hue = polar + radius * 2 / width - t * 0.22;
                light = 0.3 + 0.7 * (0.5 + 0.5 * Math.Sin((polar * 3 - radius * 5 / width + t * 0.4) * Tau));
                break;
        }
        if (!_options.Rainbow && _kind is PatternKind.Wave or PatternKind.Rainbow)
            light = 0.15 + 0.85 * (0.5 + 0.5 * Math.Sin(hue * Tau));
        return Colour(hue, light, saturation);
    }

    private Rgb ReactivePixel(int index, double seconds, in EffectContext ctx)
    {
        var key = ctx.Layout.ByIndex(index);
        Rgb result = Rgb.Black;
        foreach (var hit in _hits)
        {
            double age = Math.Max(0, seconds - hit.When.TotalSeconds) * _options.Speed;
            double fade = Math.Max(0, 1 - age / 2);
            var origin = ctx.Layout.ByIndex(hit.KeyIndex);
            double dr = key.Row - origin.Row, dc = key.Col - origin.Col;
            if (_options.MirrorX) dc = -dc;
            if (_options.MirrorY) dr = -dr;
            double light;
            if (_kind == PatternKind.PressFade)
                light = hit.KeyIndex == index ? fade : 0;
            else
                light = Math.Max(0, 1 - Math.Abs(Math.Sqrt(dr * dr + dc * dc) - age * 9) / _options.Width) * fade;
            result = Compositor.Blend(result, Colour(hit.KeyIndex * 0.071 + age * 0.2, light), BlendMode.Max);
        }
        return result;
    }

    private Rgb Colour(double hue, double intensity, double saturation = 1) => _options.Rainbow
        ? Rgb.FromHsv(hue, _options.Saturation * saturation, Math.Clamp(intensity, 0, 1))
        : _color.Scale((float)Math.Clamp(intensity, 0, 1));
    private static double Safe(double v, double min, double max, double fallback) => double.IsFinite(v) ? Math.Clamp(v, min, max) : fallback;
    private static double Frac(double x) => x - Math.Floor(x);
    private static double Hash(double x) => Frac(Math.Sin(x * 12.9898 + 78.233) * 43758.5453);
    private static double Triangle(double x) => 1 - Math.Abs(Frac(x) * 2 - 1);
}
