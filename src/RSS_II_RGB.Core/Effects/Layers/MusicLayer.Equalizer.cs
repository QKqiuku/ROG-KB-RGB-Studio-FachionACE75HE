using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Effects.Layers;

public sealed partial class MusicLayer
{
    private const double EqualizerGravity = 2.8;
    private readonly double[] _capHeight = new double[32];
    private readonly double[] _capVelocity = new double[32];
    private readonly double[] _previousBar = new double[32];
    private double _capTime = -1;
    private int _capBins;

    private void PrepareEqualizer(AudioFrame frame, double now, bool reset)
    {
        if (_kind != MusicKind.ClassicEqualizer) return;
        if (reset)
        {
            Array.Clear(_capHeight);
            Array.Clear(_capVelocity);
            Array.Clear(_previousBar);
            _capTime = -1;
            _capBins = 0;
        }
        double dt = _capTime < 0 ? 0 : Math.Max(0, now - _capTime) * _options.Speed;
        bool audible = DisplayLevel(frame.Volume) > .001;
        int bins = Math.Min(32, frame.Spectrum.Length);
        _capBins = Math.Max(_capBins, bins);
        for (int i = 0; i < _capBins; i++)
        {
            double bar = audible && i < bins
                ? DisplayLevel(frame.Spectrum[i])*1.2 : 0;
            double height = _capHeight[i] + _capVelocity[i] * dt - .5 * EqualizerGravity * dt * dt;
            double velocity = _capVelocity[i] - EqualizerGravity * dt;
            double rise = bar - _previousBar[i];
            if (height <= bar)
            {
                height = bar;
                // A rising column transfers its speed only when it catches the falling cap.
                velocity = bar > .001 && rise > .0001 && dt > 0
                    ? Math.Clamp(rise / dt * .65, .35, 1.3) : 0;
            }
            _capHeight[i] = height;
            _capVelocity[i] = velocity;
            _previousBar[i] = bar;
        }
        _capTime = now;
    }

    private bool TryEqualizerCap(double x, double y, double rowStep, double aspect, double energy, out Rgb color)
    {
        color = Rgb.Black;
        if (_capBins == 0 || x < 0 || x > 1 || y < -.2 || y > 1) return false;
        int bin = Math.Min(_capBins-1,(int)(x*_capBins));
        double height = _capHeight[bin];
        if (height <= .001) return false;
        double binWidth = _capBins == 1 ? 1.0 / 32 : 1.0 / _capBins;
        double thickness = Math.Min(.12, binWidth * Math.Max(.01, aspect)*.76);
        if(_capBins>1 && rowStep<thickness)
        {
            double position=x*_capBins-bin;
            if(position<.12 || position>.88)return false;
        }
        // Nearest-row snapping preserves caps on keyboards with only a few LED rows.
        double capY = Math.Clamp(1 - height, -.2, 1);
        if (rowStep >= thickness) capY = Math.Round(capY / rowStep) * rowStep;
        if (Math.Abs(y - capY) > Math.Max(rowStep * .5, thickness * .5) + 1e-9) return false;
        color = Rgb.White;
        return true;
    }
}
