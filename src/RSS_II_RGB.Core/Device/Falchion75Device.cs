using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Device;

/// <summary>
/// M705 protocol over an owned asynchronous HID stream. Calls are serialized by
/// the render lifecycle; the Windows factory supplies the native handle.
/// </summary>
public sealed class Falchion75Device : IKeyboardDevice
{
    private readonly Stream _stream;
    private readonly byte[] _frame = new byte[Falchion75Layout.DirectPacketsPerFrame * CoreConstants.ReportLength];
    private readonly byte[] _reply = new byte[CoreConstants.ReportLength];
    private bool _softwareModeReady;
    private bool _restoreSoftwareMode;
    private bool _disposed;

    public Falchion75Device(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanWrite)
        {
            throw new ArgumentException("A readable, writable HID stream is required.", nameof(stream));
        }
        _stream = stream;
    }

    public bool IsOpen => !_disposed && _stream.CanRead && _stream.CanWrite;
    public KeyboardProfile Profile => Falchion75Layout.Profile;

    public async Task WriteFrameAsync(LedFrame frame, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ct.ThrowIfCancellationRequested();
        bool hasDecorativePixels = frame.Length == Profile.LedCount + Falchion75Layout.LightBarLedCount;
        if (frame.Length != Profile.LedCount && !hasDecorativePixels) throw new ArgumentException("Unexpected lighting frame size.", nameof(frame));
        DirectProtocol.BuildFrame(Profile, frame.Pixels[..Profile.LedCount], _frame);
        for (int i = 0; i < Falchion75Layout.LightBarLedCount; i++)
        {
            int entry = Profile.LedCount + i;
            int packet = entry / CoreConstants.LedsPerPacket * CoreConstants.ReportLength;
            int slot = entry % CoreConstants.LedsPerPacket;
            _frame[packet + 1] = CoreConstants.DirectHeaderHi;
            _frame[packet + 2] = CoreConstants.DirectHeaderLo;
            _frame[packet + 3] = (byte)(slot + 1);
            int offset = packet + 5 + slot * 4;
            var color = hasDecorativePixels ? frame[Profile.LedCount + i] : frame[Falchion75Layout.LightBarSourceIndices[i]];
            _frame[offset] = Falchion75Layout.LightBarIds[i];
            _frame[offset + 1] = color.R;
            _frame[offset + 2] = color.G;
            _frame[offset + 3] = color.B;
        }
        if (!_softwareModeReady)
        {
            await CommandAsync(CoreConstants.Falchion75QuerySoftwareMode, 0, ct).ConfigureAwait(false);
            if (_reply[5] != 0)
            {
                throw new InvalidOperationException("Keyboard software mode is already active; close other lighting controllers.");
            }
            // Restore even if the mode-entry acknowledgement is lost.
            _restoreSoftwareMode = true;
            await CommandAsync(CoreConstants.Falchion75SetSoftwareMode, 1, ct).ConfigureAwait(false);
            await CommandAsync(CoreConstants.Falchion75QuerySoftwareMode, 0, ct).ConfigureAwait(false);
            if (_reply[5] != 1)
            {
                throw new IOException("Keyboard software mode did not enable.");
            }
            _softwareModeReady = true;
        }
        for (int p = 0; p < Falchion75Layout.DirectPacketsPerFrame; p++)
        {
            await ExchangeAsync(_frame.AsMemory(p * CoreConstants.ReportLength, CoreConstants.ReportLength), ct)
                .ConfigureAwait(false);
        }
    }

    public async Task<DeviceInfo> ReadInfoAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await CommandAsync(CoreConstants.CmdQuery, 0, ct).ConfigureAwait(false);
        return new DeviceInfo($"{_reply[7]:X}.{_reply[6]:X2}.{_reply[5]:X2}", -1);
    }

    public Task ApplyHardwareEffectAsync(HardwareEffect effect, CancellationToken ct = default)
        => Task.FromException(new NotSupportedException("Hardware preset writes are not enabled for experimental M705 support."));

    public Task SaveToFlashAsync(CancellationToken ct = default)
        => Task.FromException(new NotSupportedException("Flash writes are disabled for experimental M705 support."));

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        try
        {
            if (_restoreSoftwareMode)
            {
                await CommandAsync(CoreConstants.Falchion75SetSoftwareMode, 0, CancellationToken.None).ConfigureAwait(false);
                await CommandAsync(CoreConstants.Falchion75QuerySoftwareMode, 0, CancellationToken.None).ConfigureAwait(false);
                if (_reply[5] != 0) throw new IOException("Keyboard software mode restoration failed.");
            }
        }
        finally
        {
            _disposed = true;
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    private Task CommandAsync(byte command, byte value, CancellationToken ct)
    {
        var report = new byte[CoreConstants.ReportLength];
        report[1] = command;
        report[5] = value;
        return ExchangeAsync(report, ct);
    }

    private async Task ExchangeAsync(ReadOnlyMemory<byte> report, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(CoreConstants.Falchion75ResponseTimeoutMs);
        try
        {
            await _stream.WriteAsync(report, timeout.Token).ConfigureAwait(false);
            while (true)
            {
                await _stream.ReadExactlyAsync(_reply, timeout.Token).ConfigureAwait(false);
                byte expectedSubcommand = report.Span[1] == CoreConstants.DirectHeaderHi
                    ? CoreConstants.Falchion75DirectAck : report.Span[2];
                if (_reply[0] == report.Span[0] && _reply[1] == report.Span[1] && _reply[2] == expectedSubcommand
                    && _reply[3] == report.Span[3] && _reply[4] == report.Span[4])
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Keyboard did not acknowledge the HID report before the deadline.");
        }
    }
}
