namespace RSS_II_RGB.App;

internal sealed class ShutdownCleanup(IAsyncDisposable keyboard, IAsyncDisposable sensors)
{
    private Task? _stop;

    // All requests originate on the UI thread; cleanup runs without that context
    // so the synchronous OS shutdown event can wait for the same task safely.
    public Task StopAsync() => _stop ??= StopCoreAsync();

    private async Task StopCoreAsync()
    {
        try
        {
            await keyboard.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            await sensors.DisposeAsync().ConfigureAwait(false);
        }
    }
}
