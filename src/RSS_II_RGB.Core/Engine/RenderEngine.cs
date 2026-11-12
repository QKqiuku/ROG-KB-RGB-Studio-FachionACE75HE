using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RSS_II_RGB.Core.Device;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Input;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Engine;

/// <summary>
/// The software render loop. On a background task it drains key events, composites
/// the layer stack into a frame, and streams it to the device — paced to a target
/// FPS (the device write is the real throttle). Key events arrive lock-free from
/// the hook thread.
/// </summary>
public sealed class RenderEngine : IAsyncDisposable
{
    private readonly IKeyboardDevice _device;
    private readonly Compositor _compositor;
    private readonly ILogSink _log;
    private readonly KeyboardProfile _profile;
    private int _targetFps;
    private readonly IFrameTiming? _timing;

    private readonly LedFrame _frame;
    private Rgb[] _lastSentFrame = [];
    private readonly ConcurrentQueue<KeyEvent> _keyQueue = new();
    private readonly List<KeyHit> _hits = new();
    private readonly bool[] _heldKeys; // per-LED down state, to drop OS auto-repeat

    // Set from any thread; applied to the compositor on the engine thread.
    private sealed record PendingLayers(IReadOnlyList<IEffectLayer> Keys, IReadOnlyList<IEffectLayer> Spatial, DecorationMode Bar, DecorationMode Eye, OutputEnvelope? Envelope);
    private PendingLayers? _pendingLayers;
    private readonly Falchion75Lighting? _lighting;
    private OutputEnvelope? _envelope;

    public RenderEngine(IKeyboardDevice device, Compositor compositor, ILogSink log,
                        KeyboardProfile profile, int targetFps = CoreConstants.DefaultTargetFps, IFrameTiming? timing = null)
    {
        _device = device;
        _compositor = compositor;
        _log = log;
        _profile = profile;
        _frame = new LedFrame(profile.LedCount);
        if (ReferenceEquals(profile, Falchion75Layout.Profile)) _lighting = new Falchion75Lighting();
        _heldKeys = new bool[profile.LedCount];
        _targetFps = Math.Clamp(targetFps, 1, 240);
        _timing = timing;
    }

    public Compositor Compositor => _compositor;
    public int TargetFps => Volatile.Read(ref _targetFps);
    public void SetTargetFps(int targetFps) => Volatile.Write(ref _targetFps, Math.Clamp(targetFps, 1, 240));

    /// <summary>Frames streamed so far (diagnostics / FPS checks).</summary>
    public long FramesRendered { get; private set; }
    public Exception? LastWriteError { get; private set; }
    public event Action? FirstFrameSent;
    private long _lastSentUtcTicks;
    public DateTimeOffset? LastFrameSentAt => Interlocked.Read(ref _lastSentUtcTicks) is > 0 and var ticks ? new DateTimeOffset(ticks, TimeSpan.Zero) : null;
    private long _lastFrameElapsedTicks;
    public TimeSpan LastFrameElapsed=>TimeSpan.FromTicks(Interlocked.Read(ref _lastFrameElapsedTicks));

    /// <summary>Immutable snapshot of the last fully sent frame; empty before the first successful write.</summary>
    public ReadOnlyMemory<Rgb> LastSentFrame
    {
        get { var snapshot = Volatile.Read(ref _lastSentFrame); return snapshot.AsMemory(0, Math.Min(snapshot.Length, _profile.LedCount)); }
    }
    public ReadOnlyMemory<Rgb> LastSentLightingFrame => Volatile.Read(ref _lastSentFrame);

    /// <summary>Thread-safe; called from the hook thread.</summary>
    public void EnqueueKeyEvent(in KeyEvent keyEvent) => _keyQueue.Enqueue(keyEvent);

    /// <summary>
    /// Atomically swap the active effect stack. Thread-safe: the UI thread sets
    /// the pending list; the engine thread installs it at the next tick, so the
    /// compositor is only ever mutated on the engine thread.
    /// </summary>
    public void SetEffectLayers(IReadOnlyList<IEffectLayer> layers, IReadOnlyList<IEffectLayer>? spatialLayers = null,
        DecorationMode bar = DecorationMode.FollowKeys, DecorationMode eye = DecorationMode.FollowKeys, OutputEnvelope? envelope = null)
        => Interlocked.Exchange(ref _pendingLayers, new PendingLayers(layers, spatialLayers ?? [], bar, eye,envelope));

    public async Task RunAsync(CancellationToken ct)
    {
        LastWriteError = null;
        using var timing = _timing?.Enter();
        int targetFps = TargetFps;
        var period = TimeSpan.FromSeconds(1.0 / targetFps);
        var clock = Stopwatch.StartNew();
        TimeSpan previous = TimeSpan.Zero;
        TimeSpan due = TimeSpan.Zero;

        _log.Log(LogLevel.Info, $"Render loop started ({targetFps} FPS target).");

        while (!ct.IsCancellationRequested)
        {
            int requestedFps = TargetFps;
            if (requestedFps != targetFps)
            {
                targetFps = requestedFps;
                period = TimeSpan.FromSeconds(1.0 / targetFps);
                due = clock.Elapsed;
            }
            TimeSpan now = clock.Elapsed;
            TimeSpan delta = now - previous;
            previous = now;

            ApplyPendingLayers();
            DrainKeyEvents(now);

            var ctx = new EffectContext(now, delta, CollectionsMarshal.AsSpan(_hits), _profile);
            _compositor.Compose(_frame, ctx);
            var output = _lighting?.Compose(_frame, ctx) ?? _frame;
            _envelope?.Apply(output.Pixels,now);

            try
            {
                await _device.WriteFrameAsync(output, ct).ConfigureAwait(false);
                // Publish only after every packet succeeds; the render buffer is reused next tick.
                Volatile.Write(ref _lastSentFrame, output.Pixels.ToArray());
                Interlocked.Exchange(ref _lastFrameElapsedTicks,now.Ticks);
                Interlocked.Exchange(ref _lastSentUtcTicks, DateTimeOffset.UtcNow.Ticks);
                FramesRendered++;
                if (FramesRendered == 1) FirstFrameSent?.Invoke();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // A failed write means the device is gone (unplugged). Log once and
                // stop the loop; the controller treats the engine exit as a lost
                // device and re-enters its detect/reconnect poll.
                LastWriteError = ex;
                _log.Log(LogLevel.Warning, "Keyboard lighting write failed.", ex);
                break;
            }

            due += period;
            TimeSpan remaining = due - clock.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Ceiling(remaining.TotalMilliseconds)), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            else due = clock.Elapsed; // Skip missed deadlines; never queue frames behind the device.
        }

        _log.Log(LogLevel.Info, $"Render loop stopped after {FramesRendered} frames.");
    }

    private void ApplyPendingLayers()
    {
        PendingLayers? pending = Interlocked.Exchange(ref _pendingLayers, null);
        if (pending is null)
        {
            return;
        }

        _lighting?.Configure(pending.Spatial, pending.Bar, pending.Eye);
        _envelope=pending.Envelope;
        _compositor.Clear();
        foreach (IEffectLayer layer in pending.Keys)
        {
            _compositor.Add(layer);
        }
    }

    private void DrainKeyEvents(TimeSpan now)
    {
        _hits.Clear();
        while (_keyQueue.TryDequeue(out KeyEvent ke))
        {
            if ((uint)ke.KeyIndex >= (uint)_heldKeys.Length)
            {
                continue; // no LED for this key
            }

            if (ke.IsDown)
            {
                // The first press becomes a reactive hit; the OS sends repeated
                // key-downs while a key is held (auto-repeat / continuous input),
                // which we drop so a held key doesn't keep re-triggering the effect.
                if (!_heldKeys[ke.KeyIndex])
                {
                    _heldKeys[ke.KeyIndex] = true;
                    _hits.Add(new KeyHit(ke.KeyIndex, now));
                }
            }
            else
            {
                _heldKeys[ke.KeyIndex] = false; // released — the next press hits again
            }
        }
    }

    public ValueTask DisposeAsync() => _device.DisposeAsync();
}
