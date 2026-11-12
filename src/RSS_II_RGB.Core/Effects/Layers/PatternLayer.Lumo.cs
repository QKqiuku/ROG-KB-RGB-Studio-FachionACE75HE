using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Effects.Layers;

public sealed partial class PatternLayer
{
    // Independent procedural fields: no reference animation frames are shipped or replayed.
    private bool TryLumoPixel(double u, double v, double t, in EffectContext ctx, out Rgb result)
    {
        double w = _options.Width, d = _options.Density;
        double x = u - .5, y = v - .5;
        double r = Math.Sqrt(x * x + y * y), a = Math.Atan2(y, x) / Tau;
        double h = 0, light = 1, sat = 1;
        switch (_kind)
        {
            case PatternKind.Marquee:
                double stripe = .5 + .5 * Math.Sin((u * 2.2 + v * .65) * Tau / w - t * 2);
                double blend = Math.Clamp((stripe - .15) / .7, 0, 1);
                result = _options.Rainbow
                    ? Rgb.Lerp(Colour(.51, 1), Colour(.86, 1), (float)blend)
                    : Colour(0, .25 + .75 * stripe);
                return true;
            case PatternKind.LakePixels:
            case PatternKind.LakeRipples:
            case PatternKind.LakeSurge:
                // The global hue evolves together while each surface has its own motion field.
                double surface;
                if (_kind == PatternKind.LakePixels)
                {
                    double seed = Math.Floor(u * (ctx.Layout.Cols - 1) + .5) * 31
                        + Math.Floor(v * (ctx.Layout.Rows - 1) + .5) * 137;
                    double phase = t * .8 / w + Hash(seed) * Tau;
                    surface = Math.Pow(.5 + .5 * Math.Sin(phase), 2);
                }
                else if (_kind == PatternKind.LakeRipples)
                    surface = .5 + .5 * Math.Sin(r * 21 / w - t * 2 + Math.Sin(a * 3 + t * .4));
                else
                    surface = .5 + .5 * Math.Sin((u * 7 + v * 9) / w + Math.Sin(v * 8 - t) * 2 - t * 1.6);
                h = .28 - t * .055 + surface * .035;
                light = .06 + .94 * surface; sat = .72 + .28 * surface;
                break;
            case PatternKind.SineWave:
                double line = .5 + .32 * Math.Sin((u / w - t * .23) * Tau);
                light = Math.Exp(-Math.Pow((v - line) / (.105 * w), 2));
                h = u * .95 - t * .14;
                break;
            case PatternKind.PerimeterFlow:
                // A rectangular radius distorts the angular colour field across the whole board.
                double edge = Math.Max(Math.Abs(x), Math.Abs(y));
                h = a + edge * .65 / w - t * .19;
                light = _options.Rainbow ? 1 : .25 + .75 * (.5 + .5 * Math.Sin(h * Tau));
                break;
            case PatternKind.RotatingRing:
            case PatternKind.WindmillCycle:
                h = a / w - t * .18;
                if (_kind == PatternKind.WindmillCycle) { h += .12 * Math.Sin(a * Tau * 3 - t); sat = .65; }
                light = _options.Rainbow ? 1 : .18 + .82 * (.5 + .5 * Math.Sin(h * Tau));
                break;
            case PatternKind.DoubleBeacon:
                double beacon = Math.Pow(Math.Abs(Math.Cos((a - t * .16) * Tau)), 2 / w);
                h = a - t * .16 + beacon * .12;
                light = .80 + .20 * beacon;
                break;
            case PatternKind.SineSectors:
                // The preview curves the rotating wave radially and keeps a dim pink floor.
                double sectorY = y * .37;
                double sectorRadius = Math.Sqrt(x * x + sectorY * sectorY);
                double sector = Math.Atan2(sectorY, x) + sectorRadius * 6 / w - t * 2.31 + .55;
                // shortcut: constant-speed fit approximates the preview's varying first rotation; refine if source timing becomes available.
                light = .06 + .94 * Math.Pow(Math.Clamp(.37 + .705 * Math.Sin(sector), 0, 1), .825 / w);
                h = sector / Tau;
                sat = 1;
                break;
            case PatternKind.Caustics:
                double water = Math.Sin(u * 17 / w + Math.Sin(v * 9 + t))
                    + Math.Sin(v * 14 / w - t * 1.3 + Math.Sin(u * 11 - t * .7));
                double pool = Math.Pow(Math.Max(0, water * .5), 2);
                h = .62 - pool * .11; light = .44 + .56 * pool; sat = 1 - pool * .55;
                break;
            case PatternKind.Sun:
                double sunX = .5 + .10 * Math.Sin(t * .55), sunY = .5 + .12 * Math.Cos(t * .7);
                double sunR = Math.Sqrt(Math.Pow(u - sunX, 2) + Math.Pow(v - sunY, 2));
                double halo = Math.Exp(-sunR * sunR / (.14 * w * w));
                h = .025 + halo * .09; light = .28 + .72 * halo; sat = 1 - halo * .6;
                break;
            case PatternKind.Sakura:
                light = 0; h = .92; sat = .45;
                for (int p = 0; p < 3 + (int)(d * 17); p++)
                {
                    double py = Frac(t * (.10 + Hash(p * 23) * .10) + Hash(p * 13));
                    double px = Frac(Hash(p * 17) + .05 * Math.Sin(t + p) + py * .12);
                    double petal = Math.Exp(-Math.Pow((u - px) / (.032 * w), 2)
                        - Math.Pow((v - py) / (.08 * w), 2));
                    light = Math.Max(light, petal * (.45 + .55 * Hash(p * 7)));
                }
                break;
            case PatternKind.IceCrystal:
                double cell = Math.Floor(u * (ctx.Layout.Cols - 1) + .5) * 29
                    + Math.Floor(v * (ctx.Layout.Rows - 1) + .5) * 97;
                double sparkle = Math.Pow(Math.Max(0, Math.Sin(t * (1 + Hash(cell)) + Hash(cell + 7) * Tau)), 18);
                sparkle *= Hash(cell + 13) < d ? 1 : .12;
                double facet = .5 + .5 * Math.Sin((u * 8 + v * 5) / w - t * .6);
                h = .61 - sparkle * .10; light = .60 + facet * .15 + sparkle * .25; sat = .85 * (1 - sparkle);
                break;
            case PatternKind.AuroraRipples:
                double interference = .5 + .25 * (Math.Sin(u * 9 / w + v * 5 - t)
                    + Math.Sin(v * 10 / w - u * 4 + t * .8));
                h = .50 + .37 * interference; light = .35 + .65 * interference; sat = .3 + .35 * interference;
                break;
            default:
                result = default;
                return false;
        }
        result = Colour(h, light, sat);
        return true;
    }
}
