namespace RSS_II_RGB.Core.Audio;

public static class AudioFrameTransport
{
    // Fixed layout bounds the existing numeric sensor protocol without reflection.
    public static double[] Pack(AudioFrame f)
    {
        double[] values = new double[239];
        double[] header = [f.Sequence, f.Generation, f.TimestampMs, f.Volume, f.Bass, f.Mid, f.Treble, f.Onset, f.BeatId, f.LeftLevel, f.RightLevel];
        header.CopyTo(values, 0);
        f.Spectrum.AsSpan(0, Math.Min(32, f.Spectrum.Length)).CopyTo(values.AsSpan(11));
        f.LeftSpectrum.AsSpan(0, Math.Min(32, f.LeftSpectrum.Length)).CopyTo(values.AsSpan(43));
        f.RightSpectrum.AsSpan(0, Math.Min(32, f.RightSpectrum.Length)).CopyTo(values.AsSpan(75));
        f.Waveform.AsSpan(0, Math.Min(128, f.Waveform.Length)).CopyTo(values.AsSpan(107));
        values[235] = f.BassBeatId; values[236] = f.LowBeatId; values[237] = f.BassOnset; values[238] = f.LowOnset;
        return values;
    }
    public static AudioFrame Decode(double[] v)
    {
        if ((v.Length != 235 && v.Length != 239) || v.Any(x => !double.IsFinite(x)) || v.Take(3).Any(x => x < 0 || x > 9_007_199_254_740_991) || v[8] < 0 || v[8] > 9_007_199_254_740_991 || v.Skip(3).Take(5).Any(x => x < 0 || x > 1) || v.Skip(9).Take(98).Any(x => x < 0 || x > 1) || v.Skip(107).Take(128).Any(x => Math.Abs(x) > 1)) return AudioFrame.Empty;
        if (v.Length == 239 && (v.Skip(235).Take(2).Any(x => x < 0 || x > 9_007_199_254_740_991 || x != Math.Truncate(x)) || v.Skip(237).Any(x => x < 0 || x > 1))) return AudioFrame.Empty;
        return new() { Sequence = (long)v[0], Generation = (long)v[1], TimestampMs = (long)v[2], Volume = v[3], Bass = v[4], Mid = v[5], Treble = v[6], Onset = v[7], BeatId = (long)v[8], LeftLevel = v[9], RightLevel = v[10], Spectrum = v[11..43], LeftSpectrum = v[43..75], RightSpectrum = v[75..107], Waveform = v[107..235], BassBeatId = v.Length == 239 ? (long)v[235] : 0, LowBeatId = v.Length == 239 ? (long)v[236] : 0, BassOnset = v.Length == 239 ? v[237] : 0, LowOnset = v.Length == 239 ? v[238] : 0 };
    }
}
