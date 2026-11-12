using System.Threading.Channels;
using RSS_II_RGB.Core.Device;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class RenderPreviewTests
{
    [Fact]
    public async Task FrameRateCanChangeWhileTheSameEngineKeepsRunning()
    {
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var device = new GatedDevice();
        await using var engine = new RenderEngine(device, new Compositor(83), new QuietLog(), device.Profile, 10);
        Task run = engine.RunAsync(stop.Token);
        try
        {
            async Task<double> CompleteFrames(int count)
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                for (int n = 0; n < count; n++)
                    (await device.Writes.Reader.ReadAsync(stop.Token)).SetResult();
                return clock.Elapsed.TotalSeconds / count;
            }
            double slowInterval = await CompleteFrames(5);
            engine.SetTargetFps(100);
            double fastInterval = await CompleteFrames(7);
            Assert.True(fastInterval < slowInterval * 0.7, $"Frame intervals did not shrink: {slowInterval} -> {fastInterval}");
            Assert.False(run.IsCompleted);
        }
        finally
        {
            stop.Cancel();
            await run;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FrameTimingLeaseIsReleasedOnCancellationOrDeviceFailure(bool failWrite)
    {
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var device = new GatedDevice();
        var timing = new CountingTiming();
        await using var engine = new RenderEngine(device, new Compositor(83), new QuietLog(), device.Profile, 60, timing);
        Task run = engine.RunAsync(stop.Token);
        var write = await device.Writes.Reader.ReadAsync(stop.Token);
        Assert.Equal(1, timing.ActiveLeases);
        if (failWrite) write.SetException(new IOException("Lost device."));
        else stop.Cancel();
        await run;
        if (failWrite) Assert.IsType<IOException>(engine.LastWriteError);
        else Assert.Null(engine.LastWriteError);
        Assert.Equal(0, timing.ActiveLeases);
    }

    private sealed class CountingTiming : IFrameTiming
    {
        public int ActiveLeases { get; private set; }
        public IDisposable Enter() { ActiveLeases++; return new Lease(this); }
        private sealed class Lease(CountingTiming owner) : IDisposable
        {
            public void Dispose() => owner.ActiveLeases--;
        }
    }
    [Fact]
    public async Task PreviewWaitsForCompletedWritesAndSnapshotsDoNotChangeOnTheNextFrame()
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        stop.CancelAfter(TimeSpan.FromSeconds(5));
        var device = new GatedDevice();
        await using var engine = new RenderEngine(device, new Compositor(83), new QuietLog(), device.Profile, 240);
        engine.SetEffectLayers([new SolidLayer("base", new Rgb(200, 100, 50), KeyMask.All)]);
        int firstFrameSignals = 0;
        engine.FirstFrameSent += () => firstFrameSignals++;
        Task run = engine.RunAsync(stop.Token);
        try
        {
            var first = await device.Writes.Reader.ReadAsync(stop.Token);
            Assert.True(engine.LastSentFrame.IsEmpty);
            Assert.Equal(0, firstFrameSignals);
            Assert.Null(engine.LastFrameSentAt);
            engine.SetEffectLayers([new SolidLayer("base", new Rgb(0, 0, 255), KeyMask.All)]);
            first.SetResult();
            var second = await device.Writes.Reader.ReadAsync(stop.Token);
            var red = engine.LastSentFrame;
            Assert.Equal(1, firstFrameSignals);
            Assert.NotNull(engine.LastFrameSentAt);
            Assert.Equal(83, red.Length);
            Assert.All(red.ToArray(), p => Assert.Equal(new Rgb(200, 100, 50), p));
            second.SetResult();
            var third = await device.Writes.Reader.ReadAsync(stop.Token);
            Assert.Equal(1, firstFrameSignals);
            Assert.All(engine.LastSentFrame.ToArray(), p => Assert.Equal(new Rgb(0, 0, 255), p));
            Assert.All(red.ToArray(), p => Assert.Equal(new Rgb(200, 100, 50), p));
        }
        finally
        {
            stop.Cancel();
            await run;
        }
    }

    [Fact]
    public async Task PreviewUsesFinalCompositedColoursAndDoesNotPublishAFailedWrite()
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        stop.CancelAfter(TimeSpan.FromSeconds(5));
        var device = new GatedDevice();
        await using var engine = new RenderEngine(device, new Compositor(83), new QuietLog(), device.Profile, 240);
        engine.SetEffectLayers([
            new SolidLayer("base", new Rgb(200, 100, 50), KeyMask.All),
            new SolidLayer("brightness", new Rgb(128, 128, 128), KeyMask.All, zOrder: 1, blend: BlendMode.Multiply),
            new SolidLayer("zone", new Rgb(0, 0, 255), KeyMask.FromIndices([48]), zOrder: 2)
        ]);
        Task run = engine.RunAsync(stop.Token);
        try
        {
            (await device.Writes.Reader.ReadAsync(stop.Token)).SetResult();
            var second = await device.Writes.Reader.ReadAsync(stop.Token);
            Assert.Equal(new Rgb(100, 50, 25), engine.LastSentFrame.Span[46]);
            Assert.Equal(new Rgb(0, 0, 255), engine.LastSentFrame.Span[48]);
            var completed = engine.LastSentFrame;
            engine.SetEffectLayers([new SolidLayer("base", Rgb.White, KeyMask.All)]);
            second.SetResult();
            var failed = await device.Writes.Reader.ReadAsync(stop.Token);
            failed.SetException(new IOException("Device disconnected mid-frame."));
            await run;
            Assert.Equal(completed.ToArray(), engine.LastSentFrame.ToArray());
        }
        finally
        {
            stop.Cancel();
            await run;
        }
    }

    [Fact]
    public async Task DecorativeSnapshotIsPublishedAtomicallyOnlyAfterAcknowledgement()
    {
        using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var device=new GatedDevice();
        await using var engine=new RenderEngine(device,new Compositor(83),new QuietLog(),device.Profile,240);
        engine.SetEffectLayers([new SolidLayer("keys",new Rgb(255,0,0),KeyMask.All)],
            [new SolidLayer("spatial",new Rgb(0,0,255),KeyMask.All)],DecorationMode.Spatial,DecorationMode.FollowKeys);
        var run=engine.RunAsync(stop.Token);
        try
        {
            var first=await device.Writes.Reader.ReadAsync(stop.Token);
            Assert.True(engine.LastSentLightingFrame.IsEmpty);
            Assert.Equal(96,device.LastPixels.Length);
            Assert.Equal(new Rgb(255,0,0),device.LastPixels[0]);
            Assert.Equal(new Rgb(0,0,255),device.LastPixels[83]);
            Assert.Equal(new Rgb(255,0,0),device.LastPixels[95]);
            first.SetResult();
            var second=await device.Writes.Reader.ReadAsync(stop.Token);
            var snapshot=engine.LastSentLightingFrame;
            Assert.Equal(96,snapshot.Length);
            Assert.Equal(83,engine.LastSentFrame.Length);
            Assert.Equal(new Rgb(0,0,255),snapshot.Span[83]);
            second.SetException(new IOException("Lost acknowledgement"));
            await run;
            Assert.Equal(snapshot.ToArray(),engine.LastSentLightingFrame.ToArray());
        }
        finally { stop.Cancel();await run; }
    }

    [Fact]
    public async Task FinalEnvelopeDimsKeysAndDecorationsBeforeWriteAndPreview()
    {
        using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));var device=new GatedDevice();
        await using var engine=new RenderEngine(device,new Compositor(83),new QuietLog(),device.Profile);
        engine.SetEffectLayers([new SolidLayer("keys",new Rgb(200,0,0),KeyMask.All)],
            [new SolidLayer("strip",new Rgb(0,0,200),KeyMask.All)],DecorationMode.Custom,DecorationMode.Custom,
            new OutputEnvelope(new RSS_II_RGB.Core.Sensors.SensorState(),new() {Opacity=.5}));
        var run=engine.RunAsync(stop.Token);
        try
        {
            var first=await device.Writes.Reader.ReadAsync(stop.Token);
            Assert.Equal(new Rgb(100,0,0),device.LastPixels[0]);Assert.Equal(new Rgb(0,0,100),device.LastPixels[83]);Assert.Equal(new Rgb(0,0,100),device.LastPixels[95]);
            first.SetResult();await device.Writes.Reader.ReadAsync(stop.Token);
            Assert.Equal(device.LastPixels,engine.LastSentLightingFrame.ToArray());
        }
        finally {stop.Cancel();await run;}
    }

    private sealed class GatedDevice : IKeyboardDevice
    {
        public Rgb[] LastPixels { get; private set; } = [];
        public Channel<TaskCompletionSource> Writes { get; } = Channel.CreateUnbounded<TaskCompletionSource>();
        public bool IsOpen => true;
        public KeyboardProfile Profile => Falchion75Layout.Profile;
        public async Task WriteFrameAsync(LedFrame frame, CancellationToken ct = default)
        {
            LastPixels = frame.Pixels.ToArray();
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await Writes.Writer.WriteAsync(done, ct);
            await done.Task.WaitAsync(ct);
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task ApplyHardwareEffectAsync(HardwareEffect effect, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SaveToFlashAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DeviceInfo> ReadInfoAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class QuietLog : ILogSink
    {
        public void Log(LogLevel level, string message, Exception? exception = null) { }
    }
}
