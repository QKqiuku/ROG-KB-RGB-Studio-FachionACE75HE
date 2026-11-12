using System.Text.Json.Serialization;

namespace RSS_II_RGB.Core.Audio;

public sealed record AudioSource(int ProcessId, string Name, long StartTimeTicks = 0, string? ExecutablePath = null);
public sealed record AudioAnalysisOptions
{
    public double Sensitivity { get; init; } = 1;
    public double NoiseGate { get; init; } = .002;
    public double Attack { get; init; } = .025;
    public double Release { get; init; } = .18;
    public bool AutoGain { get; init; } = true;
    public double BassEmphasis { get; init; } = 1;
}
public sealed record AudioControlRequest
{
    public string Command { get; init; } = "status";
    public int ProcessId { get; init; }
    public long StartTimeTicks { get; init; }
    public bool CaptureEnabled { get; init; } = true;
    public AudioAnalysisOptions Options { get; init; } = new();
}
public sealed record AudioControlResponse
{
    public AudioSource[] Sources { get; init; } = [];
    public string State { get; init; } = "waiting";
    public string? Error { get; init; }
    public int SelectedProcessId { get; init; }
    public bool ProcessCaptureSupported { get; init; }
}
public interface IAudioControl
{
    Task<AudioControlResponse> SendAsync(AudioControlRequest request, CancellationToken ct = default);
}
[JsonSerializable(typeof(AudioControlRequest))]
[JsonSerializable(typeof(AudioControlResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AudioControlJsonContext : JsonSerializerContext { }
