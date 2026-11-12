using System.IO.Pipes;
using System.Text.Json;
using RSS_II_RGB.Core;
using RSS_II_RGB.Core.Audio;

namespace RSS_II_RGB.SensorsHost;

internal static class AudioControlServer
{
    public static async Task RunAsync(AudioProvider audio)
    {
        while (true)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(CoreConstants.SensorPipeName + "-audio", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                AudioControlResponse response;
                try
                {
                    byte[] data = await AudioControlProtocol.ReadAsync(pipe, timeout.Token);
                    if (data.Length > 16384) throw new InvalidDataException("Audio control request exceeds its size limit.");
                    var request = JsonSerializer.Deserialize(data, AudioControlJsonContext.Default.AudioControlRequest) ?? throw new InvalidDataException("Missing request.");
                    response = audio.Control(request);
                }
                catch (Exception ex) { response = new() { State = "error", Error = ex.Message }; }
                await AudioControlProtocol.WriteAsync(pipe, JsonSerializer.SerializeToUtf8Bytes(response, AudioControlJsonContext.Default.AudioControlResponse), timeout.Token);
            }
            catch { await Task.Delay(200); }
        }
    }
}
