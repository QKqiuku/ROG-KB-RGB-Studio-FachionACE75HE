using System.Numerics;

namespace RSS_II_RGB.Core.Audio;

/// <summary>Streaming stereo power analysis; each publication owns its arrays.</summary>
public sealed class AudioAnalyzer
{
    public const int Size = 2048, Hop = 512, Bands = 32;
    private readonly double[] _left = new double[Size], _right = new double[Size], _previous = new double[Bands], _smooth = new double[Bands];
    private int _position, _filled, _hop;
    private long _sequence, _beat, _lastBeat, _bassBeat, _lowBeat, _lastBassBeat, _lastLowBeat;
    private double _baseline, _bassBaseline, _lowBaseline, _volume, _gain = 1;
    private readonly AudioAnalysisOptions _options;
    public AudioFrame Latest { get; private set; }
    public AudioAnalyzer(long generation = 1, AudioAnalysisOptions? options = null)
    {
        _options = options ?? new();
        Validate(_options);
        Latest = new() { Generation = generation };
    }
    public static void Validate(AudioAnalysisOptions o)
    {
        static bool In(double v, double a, double b) => double.IsFinite(v) && v >= a && v <= b;
        if (!In(o.Sensitivity, .1, 10) || !In(o.NoiseGate, 0, .2) || !In(o.Attack, .001, 2) || !In(o.Release, .01, 5) || !In(o.BassEmphasis, .1, 4))
            throw new ArgumentOutOfRangeException(nameof(o));
    }
    public void Push(ReadOnlySpan<float> interleaved, int channels, int sampleRate, long timestampMs)
    {
        if (channels < 1 || channels > 32 || sampleRate < 8000 || sampleRate > 384000 || interleaved.Length > 4_000_000 || interleaved.Length % channels != 0)
            throw new ArgumentOutOfRangeException(nameof(interleaved));
        for (int i = 0; i < interleaved.Length; i += channels)
        {
            _left[_position] = Clean(interleaved[i]);
            _right[_position] = Clean(interleaved[i + (channels > 1 ? 1 : 0)]);
            _position = (_position + 1) % Size;
            _filled = Math.Min(Size, _filled + 1);
            if (++_hop >= Hop && _filled == Size) { _hop = 0; Analyze(sampleRate, timestampMs); }
        }
    }
    private static double Clean(float v) => float.IsFinite(v) ? Math.Clamp(v, -1, 1) : 0;
    private void Analyze(int rate, long timestamp)
    {
        var l = new Complex[Size]; var r = new Complex[Size]; var wave = new double[128];
        double le = 0, re = 0;
        for (int i = 0; i < Size; i++)
        {
            int p = (_position + i) % Size;
            double w = .5 - .5 * Math.Cos(2 * Math.PI * i / (Size - 1));
            le += _left[p] * _left[p]; re += _right[p] * _right[p];
            l[i] = new(_left[p] * w, 0); r[i] = new(_right[p] * w, 0);
        }
        // Select one channel by power so right-only and inverted stereo retain their waveform.
        var waveformChannel = re > le ? _right : _left;
        for (int i = 0; i < wave.Length; i++) wave[i] = waveformChannel[(_position + i * 16) % Size];
        Fft(l); Fft(r);
        double dt = Hop / (double)rate, raw = Math.Sqrt((le + re) / (2 * Size));
        double targetGain = _options.AutoGain ? Math.Clamp(.2 / Math.Max(raw, .025), 1, 4) : 1;
        _gain += (targetGain - _gain) * (1 - Math.Exp(-dt / 2));
        double gain = _gain * _options.Sensitivity;
        double[] ls = new double[Bands], rs = new double[Bands], spectrum = new double[Bands];
        double flux = 0, bassFlux = 0, midFlux = 0;
        for (int b = 0; b < Bands; b++)
        {
            double top = Math.Min(16000, rate / 2.0), ratio = top / 30;
            int lo = Math.Clamp((int)(30 * Math.Pow(ratio, b / (double)Bands) * Size / rate), 1, Size / 2 - 1);
            int hi = Math.Clamp((int)(30 * Math.Pow(ratio, (b + 1.0) / Bands) * Size / rate), lo, Size / 2 - 1);
            double lp = 0, rp = 0;
            for (int k = lo; k <= hi; k++) { lp += l[k].Magnitude * l[k].Magnitude; rp += r[k].Magnitude * r[k].Magnitude; }
            double emphasis = b < 11 ? _options.BassEmphasis : 1;
            ls[b] = Math.Clamp(Math.Sqrt(lp) * 4 / Size * gain * emphasis, 0, 1);
            rs[b] = Math.Clamp(Math.Sqrt(rp) * 4 / Size * gain * emphasis, 0, 1);
            double value = raw < _options.NoiseGate ? 0 : Math.Sqrt((ls[b] * ls[b] + rs[b] * rs[b]) / 2);
            double positiveFlux = Math.Max(0, value - _previous[b]);
            flux += positiveFlux;
            if (b < 11) bassFlux += positiveFlux;
            else if (b < 22) midFlux += positiveFlux;
            _previous[b] = value;
            _smooth[b] = Smooth(_smooth[b], value, dt); spectrum[b] = _smooth[b];
        }
        bool hit = raw >= _options.NoiseGate && flux > Math.Max(.08, _baseline * 1.8) && timestamp - _lastBeat >= 160;
        _baseline += (flux - _baseline) * (1 - Math.Exp(-dt / .6));
        if (hit) { _beat++; _lastBeat = timestamp; }
        // Detect low events from their own flux; a treble attack over sustained bass cannot trigger them.
        double lowFlux = bassFlux + midFlux * .2;
        bool bassHit = raw >= _options.NoiseGate && bassFlux > Math.Max(.08, _bassBaseline * 1.8) && timestamp - _lastBassBeat >= 160;
        bool lowHit = raw >= _options.NoiseGate && lowFlux > Math.Max(.08, _lowBaseline * 1.8) && timestamp - _lastLowBeat >= 160;
        double baselineRate = 1 - Math.Exp(-dt / .6);
        _bassBaseline += (bassFlux - _bassBaseline) * baselineRate;
        _lowBaseline += (lowFlux - _lowBaseline) * baselineRate;
        if (bassHit) { _bassBeat++; _lastBassBeat = timestamp; }
        if (lowHit) { _lowBeat++; _lastLowBeat = timestamp; }
        _volume = Smooth(_volume, raw < _options.NoiseGate ? 0 : Math.Clamp(raw * gain, 0, 1), dt);
        Latest = new() { Generation = Latest.Generation, Sequence = ++_sequence, TimestampMs = timestamp, Spectrum = spectrum, LeftSpectrum = ls, RightSpectrum = rs, Waveform = wave, Volume = _volume,
            Bass = spectrum.Take(11).Average(), Mid = spectrum.Skip(11).Take(11).Average(), Treble = spectrum.Skip(22).Average(), Onset = hit ? Math.Clamp(flux, 0, 1) : 0, BeatId = _beat, BassBeatId = _bassBeat, LowBeatId = _lowBeat, BassOnset = bassHit ? Math.Clamp(bassFlux, 0, 1) : 0, LowOnset = lowHit ? Math.Clamp(lowFlux, 0, 1) : 0, LeftLevel = Math.Sqrt(le / Size), RightLevel = Math.Sqrt(re / Size) };
    }
    private double Smooth(double old, double value, double dt) => old + (value - old) * (1 - Math.Exp(-dt / (value > old ? _options.Attack : _options.Release)));
    private static void Fft(Complex[] a)
    {
        for (int i = 1, j = 0; i < a.Length; i++)
        {
            int bit = a.Length >> 1; for (; (j & bit) != 0; bit >>= 1) j ^= bit; j ^= bit;
            if (i < j) (a[i], a[j]) = (a[j], a[i]);
        }
        for (int n = 2; n <= a.Length; n <<= 1)
        {
            Complex step = Complex.FromPolarCoordinates(1, -2 * Math.PI / n);
            for (int i = 0; i < a.Length; i += n)
            {
                Complex w = Complex.One;
                for (int j = 0; j < n / 2; j++, w *= step) { Complex u = a[i + j], v = a[i + j + n / 2] * w; a[i + j] = u + v; a[i + j + n / 2] = u - v; }
            }
        }
    }
}
