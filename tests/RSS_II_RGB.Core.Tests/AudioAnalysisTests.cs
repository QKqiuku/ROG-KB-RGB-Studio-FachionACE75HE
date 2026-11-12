using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Ipc;

namespace RSS_II_RGB.Core.Tests;

public sealed class AudioAnalysisTests
{
    private static AudioFrame Tone(int rate, double hz, bool inverted = false, bool rightOnly = false, double amplitude = .5)
    {
        var a = new AudioAnalyzer(options: new() { AutoGain = false });
        for (int h = 0; h < 80; h++)
        {
            float[] pcm = new float[1024];
            for (int i = 0; i < 512; i++) { float v = (float)(amplitude * Math.Sin(2 * Math.PI * hz * (h * 512 + i) / rate)); pcm[i * 2] = rightOnly ? 0 : v; pcm[i * 2 + 1] = inverted ? -v : v; }
            a.Push(pcm, 2, rate, 1000 + h * 512L * 1000 / rate);
        }
        return a.Latest;
    }
    [Theory]
    [InlineData(22050)] [InlineData(44100)] [InlineData(48000)] [InlineData(96000)]
    public void StereoInversionPreservesEnergy(int rate)
    {
        var normal = Tone(rate, 440); var inverted = Tone(rate, 440, true);
        Assert.Equal(normal.Volume, inverted.Volume, 10);
        Assert.Equal(normal.Spectrum.Max(), inverted.Spectrum.Max(), 10);
        Assert.Equal(normal.Waveform, inverted.Waveform);
        Assert.True(inverted.Spectrum.Max() > .1);
    }
    [Fact] public void ChannelsAndFrequencyRemainDistinct()
    {
        var right = Tone(48000, 440, rightOnly: true);
        Assert.Equal(0, right.LeftLevel); Assert.True(right.RightLevel > .2);
        Assert.True(right.RightSpectrum.Max() > right.LeftSpectrum.Max());
        Assert.Contains(right.Waveform, value => Math.Abs(value) > .1);
        var bass = Tone(48000, 100); var treble = Tone(48000, 8000);
        Assert.True(bass.Bass > bass.Treble); Assert.True(treble.Treble > treble.Bass);
    }
    [Fact] public void SilenceAndInvalidSamplesCannotTrigger()
    {
        var a = new AudioAnalyzer();
        a.Push(Enumerable.Repeat(float.NaN, 8192).ToArray(), 2, 48000, 1000);
        Assert.Equal(0, a.Latest.Volume); Assert.Equal(0, a.Latest.BeatId);
        Assert.Equal(0, a.Latest.BassBeatId); Assert.Equal(0, a.Latest.LowBeatId);
        Assert.All(a.Latest.Spectrum, x => Assert.Equal(0, x));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.Push(new float[5], 2, 48000, 1000));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AudioAnalyzer(options: new() { Attack = double.NaN }));
    }
    [Fact] public void TransportRetainsGenerationAndRejectsMalformedData()
    {
        var f = Tone(48000, 100);
        var state = new SensorState(); state.Apply(new(SensorKind.AudioFrame, AudioFrameTransport.Pack(f), f.TimestampMs));
        Assert.Equal(f.Volume, state.Audio.Volume); Assert.Equal(f.Spectrum, state.Audio.Spectrum);
        Assert.False(f.IsFresh(f.TimestampMs + 501)); Assert.False(f.IsFresh(f.TimestampMs - 1));
        Assert.Equal(42, AudioFrameTransport.Decode(AudioFrameTransport.Pack(new() { Generation = 42 })).Generation);
        var values = AudioFrameTransport.Pack(f); values[20] = double.NaN;
        Assert.Same(AudioFrame.Empty, AudioFrameTransport.Decode(values));
        Assert.Same(AudioFrame.Empty, AudioFrameTransport.Decode(new double[100000]));
    }
    [Theory]
    [InlineData(22050)] [InlineData(44100)] [InlineData(48000)] [InlineData(96000)]
    public void LowFrequencyEventsExcludeTrebleAndSurviveSteadyBass(int rate)
    {
        var a = new AudioAnalyzer(options: new() { AutoGain = false });
        long bassBeforeTreble = 0, lowBeforeTreble = 0;
        for (int h = 0; h < 160; h++)
        {
            float[] pcm = new float[1024];
            for (int i = 0; i < 512; i++)
            {
                double time = (h * 512 + i) / (double)rate;
                double value = h >= 10 ? .4 * Math.Sin(2 * Math.PI * 100 * time) : 0;
                if (h >= 100) value += .4 * Math.Sin(2 * Math.PI * 8000 * time);
                pcm[i * 2] = pcm[i * 2 + 1] = (float)value;
            }
            a.Push(pcm, 2, rate, 1000 + h * 512L * 1000 / rate);
            if (h == 99) { bassBeforeTreble = a.Latest.BassBeatId; lowBeforeTreble = a.Latest.LowBeatId; }
        }
        Assert.True(bassBeforeTreble > 0); Assert.True(lowBeforeTreble > 0);
        Assert.Equal(bassBeforeTreble, a.Latest.BassBeatId);
        Assert.Equal(lowBeforeTreble, a.Latest.LowBeatId);
        var treble = Tone(rate, 8000);
        Assert.True(treble.BeatId > 0);
        Assert.Equal(0, treble.BassBeatId); Assert.Equal(0, treble.LowBeatId);
        Assert.Equal(0, treble.BassOnset); Assert.Equal(0, treble.LowOnset);
    }
    [Fact] public void MidrangeContributesOnlyToLowEvents()
    {
        var mid = Tone(48000, 1000, amplitude: .7);
        Assert.Equal(0, mid.BassBeatId);
        Assert.True(mid.LowBeatId > 0);
        var quiet = Tone(48000, 100, amplitude: .0001);
        Assert.Equal(0, quiet.BassBeatId); Assert.Equal(0, quiet.LowBeatId);
    }
    [Fact] public void TransportPreservesLowEventsAndAcceptsLegacyFrames()
    {
        var frame = new AudioFrame { Generation = 73, BassBeatId = 5, LowBeatId = 8, BassOnset = .7, LowOnset = .4 };
        var packed = AudioFrameTransport.Pack(frame);
        Assert.Equal(239, packed.Length);
        Assert.Equal(frame, AudioFrameTransport.Decode(packed) with { Spectrum = frame.Spectrum, LeftSpectrum = frame.LeftSpectrum, RightSpectrum = frame.RightSpectrum, Waveform = frame.Waveform });
        var legacy = AudioFrameTransport.Decode(packed[..235]);
        Assert.Equal(73, legacy.Generation); Assert.Equal(0, legacy.BassBeatId); Assert.Equal(0, legacy.LowBeatId);
        Assert.Equal(0, legacy.BassOnset); Assert.Equal(0, legacy.LowOnset);
        var reset = new AudioAnalyzer(74);
        Assert.Equal(74, reset.Latest.Generation); Assert.Equal(0, reset.Latest.BassBeatId); Assert.Equal(0, reset.Latest.LowBeatId);
    }
    [Theory]
    [InlineData(235, -.1)] [InlineData(235, .5)] [InlineData(236, 9007199254740992d)]
    [InlineData(236, double.NaN)] [InlineData(237, -1)] [InlineData(238, 1.01)]
    public void TransportRejectsMalformedLowEvents(int index, double value)
    {
        var packed = AudioFrameTransport.Pack(new()); packed[index] = value;
        Assert.Same(AudioFrame.Empty, AudioFrameTransport.Decode(packed));
    }
    [Fact] public async Task ControlProtocolBoundsAndRoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        using var stream = new MemoryStream(); await AudioControlProtocol.WriteAsync(stream, [1, 2, 3], ct); stream.Position = 0;
        Assert.Equal(new byte[] { 1, 2, 3 }, await AudioControlProtocol.ReadAsync(stream, ct));
        using var bad = new MemoryStream([255, 255, 255, 127]);
        await Assert.ThrowsAsync<InvalidDataException>(() => AudioControlProtocol.ReadAsync(bad, ct));
    }
    [Fact] public void VolumeDynamicsAndGenerationReset()
    {
        Assert.True(Tone(48000, 440, amplitude: .5).Volume > Tone(48000, 440, amplitude: .05).Volume * 5);
        var a = new AudioAnalyzer(73); a.Push(new float[8192], 2, 48000, 1000);
        Assert.Equal(73, a.Latest.Generation); Assert.Equal(0, a.Latest.BeatId);
    }
    [Fact] public void ImpulsesTriggerOnceAndSustainDoesNotRepeat()
    {
        var a = new AudioAnalyzer(options: new() { AutoGain = true });
        for (int h = 0; h < 200; h++)
        {
            float[] pcm = new float[1024];
            if (h >= 10 && h < 100)
                for (int i = 0; i < 512; i++) pcm[i * 2] = pcm[i * 2 + 1] = (float)(.7 * Math.Sin(2 * Math.PI * 100 * (h * 512 + i) / 48000));
            a.Push(pcm, 2, 48000, 1000 + h * 512L * 1000 / 48000);
        }
        Assert.InRange(a.Latest.BeatId, 1, 3);
        Assert.Equal(0, a.Latest.Onset);
        Assert.InRange(a.Latest.Volume, 0, .02);
        Assert.All(a.Latest.Spectrum, value => Assert.InRange(value, 0, 1));
    }
}
