using System.IO.Pipes;
using System.Text.Json;
using RSS_II_RGB.Core;
using RSS_II_RGB.Core.Audio;

namespace RSS_II_RGB.Windows;

public sealed class NamedPipeAudioControl : IAudioControl
{
    public async Task<AudioControlResponse> SendAsync(AudioControlRequest request, CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(8));
        using var pipe = new NamedPipeClientStream(".", CoreConstants.SensorPipeName + "-audio", PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(2000, timeout.Token);
        await AudioControlProtocol.WriteAsync(pipe, JsonSerializer.SerializeToUtf8Bytes(request, AudioControlJsonContext.Default.AudioControlRequest), timeout.Token);
        return JsonSerializer.Deserialize(await AudioControlProtocol.ReadAsync(pipe, timeout.Token), AudioControlJsonContext.Default.AudioControlResponse) ?? throw new InvalidDataException("Empty audio control response.");
    }
}
