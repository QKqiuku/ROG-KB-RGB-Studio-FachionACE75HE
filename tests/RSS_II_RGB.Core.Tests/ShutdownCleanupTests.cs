using System.Collections.Concurrent;
using RSS_II_RGB.App;

namespace RSS_II_RGB.Core.Tests;

public class ShutdownCleanupTests
{
    [Fact]
    public async Task ShutdownWaitsForKeyboardBeforeSensorsAndOnlyRunsOnce()
    {
        var calls = new ConcurrentQueue<string>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var keyboard = new Resource(async () =>
        {
            calls.Enqueue("keyboard-start");
            await release.Task.ConfigureAwait(false);
            calls.Enqueue("keyboard-restored");
        });
        var sensors = new Resource(() => { calls.Enqueue("sensors-stopped"); return ValueTask.CompletedTask; });
        var cleanup = new ShutdownCleanup(keyboard, sensors);
        Task first = cleanup.StopAsync();
        Task second = cleanup.StopAsync();
        Assert.Same(first, second);
        Assert.Equal(new[] { "keyboard-start" }, calls);
        Assert.False(first.IsCompleted);
        release.SetResult();
        await first;
        Assert.Equal(new[] { "keyboard-start", "keyboard-restored", "sensors-stopped" }, calls);
    }

    [Fact]
    public async Task SensorsAreStoppedEvenIfKeyboardRestorationFails()
    {
        bool sensorsStopped = false;
        var keyboard = new Resource(() => ValueTask.FromException(new IOException("Device lost.")));
        var sensors = new Resource(() => { sensorsStopped = true; return ValueTask.CompletedTask; });
        var cleanup = new ShutdownCleanup(keyboard, sensors);
        await Assert.ThrowsAsync<IOException>(() => cleanup.StopAsync());
        Assert.True(sensorsStopped);
    }

    private sealed class Resource(Func<ValueTask> stop) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => stop();
    }
}
