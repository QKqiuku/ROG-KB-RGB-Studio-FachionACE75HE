using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using RSS_II_RGB.Core;
using RSS_II_RGB.Core.Ipc;

namespace RSS_II_RGB.SensorsHost;

// Non-AOT helper: streams SensorSample JSON lines to the main app over a named
// pipe. Reflection-heavy deps (NAudio now, LibreHardwareMonitor later) stay OUT
// of the AOT main app.
internal static class Program
{
    private static readonly AudioProvider Audio = new();
    private static ISensorProvider[] Providers = [];

    private static async Task Main(string[] args)
    {
        if (args.Contains("--audio-capture-self-test", StringComparer.Ordinal))
        {
            try
            {
                using var self = Process.GetCurrentProcess();
                using var capture = new ProcessLoopbackCapture(self.Id, self.StartTime.ToUniversalTime().Ticks, (_, _, _) => { });
                await capture.Ready.WaitAsync(TimeSpan.FromSeconds(8));
                Console.WriteLine("Process loopback activation succeeded (own silent process; no playback or audio files).");
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
            return;
        }
        Providers = [new SystemMetricsProvider(), Audio];

        // Exit if the parent app goes away (no orphaned helper holding the pipe).
        if (args.Length > 0 && int.TryParse(args[0], out int parentPid))
        {
            _ = WatchParentAsync(parentPid);
        }

        foreach (ISensorProvider provider in Providers)
        {
            provider.Start();
        }
        _ = AudioControlServer.RunAsync(Audio);

        while (true)
        {
            try
            {
                await ServeOneClientAsync();
            }
            catch
            {
                await Task.Delay(500);
            }
        }
    }

    private static async Task ServeOneClientAsync()
    {
        using var server = new NamedPipeServerStream(
            CoreConstants.SensorPipeName, PipeDirection.Out, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        await server.WaitForConnectionAsync();

        using var writer = new StreamWriter(server, new UTF8Encoding(false)) { AutoFlush = true };

        int tick = 0;
        while (server.IsConnected)
        {
            foreach (ISensorProvider provider in Providers)
            {
                if (provider != Audio && tick % 2 != 0) continue;
                foreach (SensorSample sample in provider.Poll())
                {
                    string json = JsonSerializer.Serialize(sample, SensorJsonContext.Default.SensorSample);
                    await writer.WriteLineAsync(json);
                }
            }
            tick++;
            await Task.Delay(16); // Audio ~60 Hz; unrelated metrics ~30 Hz.
        }
    }

    private static async Task WatchParentAsync(int parentPid)
    {
        try
        {
            Process parent = Process.GetProcessById(parentPid);
            await parent.WaitForExitAsync();
            Environment.Exit(0); // exit only once the parent has actually exited
        }
        catch
        {
            // Couldn't attach to the parent — keep running rather than exit.
        }
    }
}
