namespace RSS_II_RGB.Core.Audio;

/// <summary>One immutable-by-convention analysis snapshot; amplitudes are normalized to 0..1.</summary>
public sealed record AudioFrame
{
    public static readonly AudioFrame Empty = new();
    public long Sequence { get; init; }
    public long Generation { get; init; }
    public long TimestampMs { get; init; }
    public double[] Spectrum { get; init; } = [];
    public double[] LeftSpectrum { get; init; } = [];
    public double[] RightSpectrum { get; init; } = [];
    public double[] Waveform { get; init; } = [];
    public double Volume { get; init; }
    public double Bass { get; init; }
    public double Mid { get; init; }
    public double Treble { get; init; }
    public double Onset { get; init; }
    public long BeatId { get; init; }
    public long BassBeatId { get; init; }
    public long LowBeatId { get; init; }
    public double BassOnset { get; init; }
    public double LowOnset { get; init; }
    public double LeftLevel { get; init; }
    public double RightLevel { get; init; }
    public bool IsFresh(long nowMs) => TimestampMs > 0 && nowMs >= TimestampMs && nowMs - TimestampMs < 500;
}
