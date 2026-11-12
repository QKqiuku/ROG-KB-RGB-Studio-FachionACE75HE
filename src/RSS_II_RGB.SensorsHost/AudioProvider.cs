using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using RSS_II_RGB.Core.Audio;
using System.Diagnostics;
using NAudio.Wave;
using RSS_II_RGB.Core;
using RSS_II_RGB.Core.Ipc;

namespace RSS_II_RGB.SensorsHost;

/// <summary>
/// Captures system output (WASAPI loopback — no admin needed) and turns it into
/// <see cref="BandCount"/> frequency-band magnitudes via FFT. Bands stay at zero
/// when nothing is playing.
///
/// The capture self-heals across idle/sleep, default-device changes, and USB sound
/// cards whose power is toggled. Three independent signals drive a rebuild — no
/// single one is reliable on its own:
///   1. <see cref="WasapiLoopbackCapture.RecordingStopped"/> — fires when Windows
///      hard-invalidates the device (AUDCLNT_E_DEVICE_INVALIDATED).
///   2. <see cref="IMMNotificationClient"/> — fires when the default render device
///      changes, or our bound device cycles state (USB selective suspend on monitor
///      blank powers a USB DAC down/up: Active→Unplugged→Active). In that case the
///      loopback client silently zombies — it neither errors nor receives data — so
///      RecordingStopped never fires and this is the only signal we get.
///   3. A 1 Hz poll in <see cref="Poll"/> comparing the live default render endpoint
///      ID to the one we bound to — a backstop for any missed notification.
/// Any of these flips <see cref="_capturing"/> off; <see cref="Poll"/> then rebuilds
/// the capture against the (possibly new) default device, so audio reactivity
/// returns on its own without an app restart.
/// </summary>
internal sealed class AudioProvider : ISensorProvider
{
    private const int BandCount = CoreConstants.AudioBandCount;
    private const long RetryBackoffMs = 1000, HealthCheckIntervalMs = 1000;
    private AudioAnalyzer _analysis = new();
    private ProcessLoopbackCapture? _processCapture;
    private AudioControlRequest _request = new() { CaptureEnabled = false };
    private long _generation;
    private string? _error;
    private string _state = "stopped";
    private readonly object _gate = new();        // guards analysis snapshots
    private readonly object _captureLock = new();  // serialises capture create/teardown
    private WasapiLoopbackCapture? _capture;
    private MMDevice? _captureDevice;            // render endpoint the capture is bound to (disposed with _capture)
    private MMDeviceEnumerator? _enumerator;     // long-lived; hosts the device-change callback
    private DeviceNotificationClient? _notifications;
    private volatile string? _boundDeviceId;     // ID of _captureDevice, for the watchdog comparison
    private volatile bool _capturing;
    private volatile bool _disposed;
    private long _nextRetryTick;       // Environment.TickCount64 before which we won't retry
    private long _nextHealthCheckTick; // Environment.TickCount64 of the next default-endpoint poll

    public void Start()
    {
        lock (_captureLock)
        {
            if (_disposed)
            {
                return;
            }
            EnsureEnumerator();
        }
    }

    public IEnumerable<SensorSample> Poll()
    {
        lock (_captureLock)
        {
            long now = Environment.TickCount64;
            if (_request.CaptureEnabled && _request.ProcessId > 0 && _state != "unsupported")
            {
                if (!IdentityMatches(_request.ProcessId, _request.StartTimeTicks))
                {
                    _processCapture?.Dispose(); _processCapture = null;
                    if (_state != "waiting") { lock (_gate) _analysis = new(++_generation, _request.Options); }
                    _state = "waiting";
                }
                else if (_processCapture?.Error is Exception ex)
                {
                    _ = _processCapture.Ready.Exception;
                    _error = ex.Message; _state = "error";
                    _processCapture.Dispose(); _processCapture = null;
                    lock (_gate) _analysis = new(++_generation, _request.Options);
                    _nextRetryTick = now + RetryBackoffMs;
                }
                else if (_processCapture is null && now >= _nextRetryTick)
                {
                    _processCapture = new(_request.ProcessId, _request.StartTimeTicks, Push);
                    _state = "waiting";
                }
                else if (_processCapture is not null)
                {
                    _state = _processCapture.IsCapturing ? "capturing" : "waiting";
                    if (_processCapture.IsCapturing) _error = null;
                }
            }


            // Watchdog: if the capture is down (failed start, or a device that went away),
            // rebuild it once the backoff has elapsed. Poll runs ~30 Hz while the app is up.
            if (_request.CaptureEnabled && _request.ProcessId == 0 && !_capturing)
            {
                if (now >= Volatile.Read(ref _nextRetryTick))
                {
                    TryStartCapture();
                }
            }
            else if (_request.CaptureEnabled && _request.ProcessId == 0 && now >= _nextHealthCheckTick)
            {
                // Backstop for the silent-zombie case (USB suspend) where neither
                // RecordingStopped nor a device notification arrives: confirm we're still
                // bound to the live default render endpoint, otherwise force a rebuild.
                _nextHealthCheckTick = now + HealthCheckIntervalMs;
                VerifyStillOnDefaultDevice();
            }

            AudioFrame frame;
            lock (_gate) frame = _analysis.Latest;
            if (!frame.IsFresh(now)) frame = new() { Generation = frame.Generation };
            var packed = new SensorSample(SensorKind.AudioFrame, AudioFrameTransport.Pack(frame), now);
            var bands = new double[BandCount];
            for (int i = 0; i < bands.Length && frame.Spectrum.Length > 0; i++) bands[i] = frame.Spectrum[i * frame.Spectrum.Length / bands.Length];
            return [packed, new SensorSample(SensorKind.AudioBands, bands, now)];
        }
    }

    public AudioControlResponse Control(AudioControlRequest request)
    {
        lock (_captureLock)
        {
            if (request.Command == "configure")
            {
                AudioAnalyzer.Validate(request.Options);
                if (request.ProcessId < 0 || request.ProcessId > 0 && request.StartTimeTicks <= 0) throw new ArgumentException("A process identity is required.");
                DisposeCapture(); _processCapture?.Dispose(); _processCapture = null;
                _request = request; _error = null;
                lock (_gate) _analysis = new(++_generation, request.Options);
                _state = "stopped";
                if (request.CaptureEnabled)
                {
                    if (request.ProcessId == 0) TryStartCapture();
                    else if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348)) _state = "unsupported";
                    else if (!IdentityMatches(request.ProcessId, request.StartTimeTicks)) _state = "waiting";
                    else { _processCapture = new(request.ProcessId, request.StartTimeTicks, Push); _state = "waiting"; }
                }
            }
            else if (request.Command != "list" && request.Command != "status") throw new ArgumentException("Unknown command.");
            return new() { Sources = request.Command == "list" ? ListSources() : [], State = _state, Error = _error, SelectedProcessId = _request.ProcessId, ProcessCaptureSupported = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348) };
        }
    }
    private static bool IdentityMatches(int pid, long ticks)
    {
        try { using var p = Process.GetProcessById(pid); return !p.HasExited && p.StartTime.ToUniversalTime().Ticks == ticks; }
        catch { return false; }
    }
    private static AudioSource[] ListSources()
    {
        var sources = new List<AudioSource>();
        var sessionPids = new HashSet<int>();
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                using (device)
                {
                    var sessions = device.AudioSessionManager.Sessions;
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        using var session = sessions[i];
                        sessionPids.Add((int)session.GetProcessID);
                    }
                }
            }
        }
        catch { /* Process enumeration remains available without an output endpoint. */ }
        foreach (var p in Process.GetProcesses())
        {
            using (p) try
            {
                if (p.Id == Environment.ProcessId || p.ProcessName.StartsWith("RSS_II_RGB", StringComparison.OrdinalIgnoreCase)) continue;
                string? path = null; try { path = p.MainModule?.FileName; } catch { }
                sources.Add(new(p.Id, p.ProcessName, p.StartTime.ToUniversalTime().Ticks, path));
            }
            catch { }
        }
        return sources.OrderByDescending(p => sessionPids.Contains(p.ProcessId)).ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.ProcessId).Take(2048).ToArray();
    }
    private void Push(float[] pcm, int channels, int rate)
    {
        lock (_gate) _analysis.Push(pcm, channels, rate, Environment.TickCount64);
    }

    // Create the device enumerator once and register for default-device / device-state
    // change notifications. Notifications are best-effort: if registration fails we still
    // have RecordingStopped and the 1 Hz endpoint poll. Call under _captureLock.
    private void EnsureEnumerator()
    {
        _enumerator ??= new MMDeviceEnumerator();
        if (_notifications is null)
        {
            try
            {
                var client = new DeviceNotificationClient(this);
                _enumerator.RegisterEndpointNotificationCallback(client);
                _notifications = client;
            }
            catch
            {
                _notifications = null;
            }
        }
    }

    // (Re)create the loopback capture against the current default render device.
    private void TryStartCapture()
    {
        lock (_captureLock)
        {
            if (_disposed)
            {
                return;
            }

            EnsureEnumerator();
            DisposeCapture(); // tear down any previous (possibly dead) instance first
            try
            {
                // Bind explicitly to the default render endpoint so we can remember its ID
                // and detect later when the system default moves out from under us.
                MMDevice device = _enumerator!.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                var capture = new WasapiLoopbackCapture(device);
                _captureDevice = device;
                _boundDeviceId = device.ID;

                capture.DataAvailable += OnDataAvailable;
                capture.RecordingStopped += OnRecordingStopped;
                _capture = capture;
                capture.StartRecording();
                ResetAnalysis();
                _capturing = true;
                _state = "capturing";
                _error = null;
            }
            catch (Exception ex)
            {
                _error = ex.Message; _state = "error";
                DisposeCapture();
                // No render device right now (e.g. the USB card is still powering up).
                // Stay dark and try again after the backoff.
                _capture = null;
                _captureDevice = null;
                _boundDeviceId = null;
                _capturing = false;
                Volatile.Write(ref _nextRetryTick, Environment.TickCount64 + RetryBackoffMs);
            }
        }
    }

    // The capture thread stopped — device hard-invalidated or errored. Go dark and let
    // Poll rebuild it. Disposal happens in TryStartCapture (disposing from this handler's
    // own thread can deadlock NAudio).
    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }
        RequestRestart();
    }

    // Flip the capture off and ask Poll to rebuild ASAP. Safe to call from any thread
    // (the capture thread, the COM notification thread, or the Poll thread): it only
    // touches volatile flags and _bands, never COM or _captureLock, so it can't deadlock.
    private void RequestRestart()
    {
        if (!_request.CaptureEnabled || _request.ProcessId != 0) return;
        _capturing = false;
        Volatile.Write(ref _nextRetryTick, 0); // rebuild on the next Poll, skip the backoff
        lock (_gate) _analysis = new(++_generation, _request.Options);
    }

    // 1 Hz backstop: if the system default render endpoint no longer matches the device
    // our capture is bound to (or there's no usable default), force a rebuild.
    private void VerifyStillOnDefaultDevice()
    {
        string? bound = _boundDeviceId;
        MMDeviceEnumerator? enumerator = _enumerator;
        if (bound is null || enumerator is null)
        {
            return; // bound without a known ID — nothing to compare against
        }

        try
        {
            using MMDevice current = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            if (!string.Equals(current.ID, bound, StringComparison.OrdinalIgnoreCase))
            {
                RequestRestart(); // default render device changed under us
            }
        }
        catch
        {
            RequestRestart(); // no usable default render endpoint right now
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (sender is not WasapiLoopbackCapture capture) return;
        var f = capture.WaveFormat;
        int bytes = f.BitsPerSample / 8;
        if (bytes != 4 && bytes != 2) { _error = "Unsupported capture sample format."; _state = "error"; return; }
        var pcm = new float[e.BytesRecorded / bytes];
        bool floating = f.Encoding == WaveFormatEncoding.IeeeFloat || f is WaveFormatExtensible x && x.SubFormat == new Guid("00000003-0000-0010-8000-00aa00389b71");
        for (int i = 0; i < pcm.Length; i++) pcm[i] = floating ? BitConverter.ToSingle(e.Buffer, i * bytes) : bytes == 2 ? BitConverter.ToInt16(e.Buffer, i * bytes) / 32768f : BitConverter.ToInt32(e.Buffer, i * bytes) / 2147483648f;
        Push(pcm, f.Channels, f.SampleRate);
    }
    private void ResetAnalysis() { lock (_gate) _analysis = new(++_generation, _request.Options); }

    // Detach and release the current capture (call under _captureLock). Unhooking the
    // events first means our own teardown never re-enters OnRecordingStopped.
    private void DisposeCapture()
    {
        WasapiLoopbackCapture? capture = _capture;
        MMDevice? device = _captureDevice;
        _capture = null;
        _captureDevice = null;
        _capturing = false;
        if (capture is not null)
        {
            capture.DataAvailable -= OnDataAvailable;
            capture.RecordingStopped -= OnRecordingStopped;
            try { capture.StopRecording(); } catch { /* device may already be gone */ }
            try { capture.Dispose(); } catch { /* ignore */ }
        }
        try { device?.Dispose(); } catch { /* ignore */ }
    }

    public void Dispose()
    {
        _disposed = true;
        lock (_captureLock)
        {
            if (_enumerator is not null && _notifications is not null)
            {
                try { _enumerator.UnregisterEndpointNotificationCallback(_notifications); } catch { /* ignore */ }
            }
            _notifications = null;
            DisposeCapture();
            _processCapture?.Dispose();
            try { _enumerator?.Dispose(); } catch { /* ignore */ }
            _enumerator = null;
        }
    }

    // Listens for default-device and device-state changes from the audio stack. Callbacks
    // arrive on a COM thread, so they only flip flags via RequestRestart — never touch
    // COM or _captureLock — and the actual rebuild happens on the Poll thread.
    private sealed class DeviceNotificationClient : IMMNotificationClient
    {
        private readonly AudioProvider _owner;

        public DeviceNotificationClient(AudioProvider owner) => _owner = owner;

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            // The system's default output moved (or a resumed USB DAC re-took default).
            if (flow == DataFlow.Render)
            {
                _owner.RequestRestart();
            }
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            // Our bound device cycled power (USB selective suspend → resume), or some device
            // (re)appeared while we were dark. Either way, rebuild to re-bind to a live endpoint.
            if (!_owner._capturing
                || string.Equals(deviceId, _owner._boundDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                _owner.RequestRestart();
            }
        }

        public void OnDeviceRemoved(string deviceId)
        {
            if (string.Equals(deviceId, _owner._boundDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                _owner.RequestRestart();
            }
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
        }
    }
}
