using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Effects.Layers;

/// <summary>Soft screen-derived washes, with no capture work on the render thread.</summary>
public sealed class ScreenPaletteLayer(string id, IScreenColorSource source, KeyMask mask,
    PatternOptions options, int zOrder = 0, Rgb? color = null) : IEffectLayer
{
    private Rgb[] _previous = [];
    public string Id => id;
    public int ZOrder => zOrder;
    public BlendMode Blend => BlendMode.Normal;
    public KeyMask Mask => mask;
    public bool IsComplete => false;

    public void Render(Span<Rgb> target, in EffectContext ctx)
    {
        var pixels = source.Colors.Span;
        int width = source.Width, height = source.Height;
        if (width <= 0 || height <= 0 || (long)width * height > pixels.Length)
        {
            target.Clear();
            _previous = [];
            return;
        }
        bool first = _previous.Length != target.Length;
        if (first) _previous = new Rgb[target.Length];
        double speed = Safe(options.Speed, 0.1, 5, 1);
        float easing = first ? 1 : (float)(1 - Math.Exp(-Math.Max(0, ctx.Delta.TotalSeconds) * speed / 0.3));
        double angle = Safe(options.Direction, 0, 360, 0) * Math.PI / 180;
        double cos = Math.Cos(angle), sin = Math.Sin(angle);
        double radius = Safe(options.Width, 0.25, 3, 1) * 1.5;
        double saturation = Safe(options.Saturation, 0, 1, 1) * 0.65;
        for (int i = 0; i < target.Length; i++)
        {
            var key = ctx.Layout.ByIndex(i);
            double x = (double)key.Col / Math.Max(1, ctx.Layout.Cols - 1);
            double y = (double)key.Row / Math.Max(1, ctx.Layout.Rows - 1);
            if (options.MirrorX) x = 1 - x;
            if (options.MirrorY) y = 1 - y;
            double u = Math.Clamp((x - 0.5) * cos + (y - 0.5) * sin + 0.5, 0, 1) * (width - 1);
            double v = Math.Clamp(-(x - 0.5) * sin + (y - 0.5) * cos + 0.5, 0, 1) * (height - 1);
            double r = 0, g = 0, b = 0, total = 0;
            for (int row = Math.Max(0, (int)Math.Floor(v - radius * 2)); row <= Math.Min(height - 1, (int)Math.Ceiling(v + radius * 2)); row++)
            for (int col = Math.Max(0, (int)Math.Floor(u - radius * 2)); col <= Math.Min(width - 1, (int)Math.Ceiling(u + radius * 2)); col++)
            {
                double weight = Math.Exp(-((col - u) * (col - u) + (row - v) * (row - v)) / (2 * radius * radius));
                var p = pixels[row * width + col];
                r += p.R * weight; g += p.G * weight; b += p.B * weight; total += weight;
            }
            r /= total; g /= total; b /= total;
            double light = (r + g + b) / 3;
            var wash = options.Rainbow
                ? new Rgb(Channel(light + (r - light) * saturation), Channel(light + (g - light) * saturation), Channel(light + (b - light) * saturation))
                : (color ?? new Rgb(110, 145, 210)).Scale((float)(Math.Max(r, Math.Max(g, b)) / 255));
            target[i] = _previous[i] = Rgb.Lerp(_previous[i], wash, easing);
        }
    }

    private static byte Channel(double value) => (byte)Math.Clamp(Math.Round(value * 0.85), 0, 255);
    private static double Safe(double value, double min, double max, double fallback)
        => double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}
