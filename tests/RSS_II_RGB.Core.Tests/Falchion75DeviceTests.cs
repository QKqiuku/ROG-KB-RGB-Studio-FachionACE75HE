using System.Threading.Channels;
using RSS_II_RGB.Core.Device;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Tests;

public class Falchion75DeviceTests
{
    [Fact]
    public async Task FullFrameWaitsForEachAckAndRestoresSoftwareModeOnDispose()
    {
        var stream = new KeyboardStream { AddNoise = true };
        var device = new Falchion75Device(stream);
        await device.WriteFrameAsync(new LedFrame(83), TestContext.Current.CancellationToken);
        Assert.Equal(1, stream.SoftwareMode);
        var packets = stream.Writes.Where(r => r[1] == 0xC0).ToArray();
        Assert.Equal(7, packets.Length);
        Assert.Equal(new byte[] { 15, 15, 15, 15, 15, 15, 6 }, packets.Select(r => r[3]));
        Assert.Equal(7, stream.DirectAcknowledgementsRead);
        await device.DisposeAsync();
        await device.DisposeAsync();
        Assert.Equal(0, stream.SoftwareMode);
        Assert.Equal(new byte[] { 1, 0 }, stream.Writes.Where(r => r[1] == 0x74).Select(r => r[5]));
        Assert.True(stream.Disposed);
        Assert.False(device.IsOpen);
    }

    [Fact]
    public async Task BarAndEyeAppendWithoutChangingAnyOfThe83KeyPayloads()
    {
        var stream = new KeyboardStream();
        await using var device = new Falchion75Device(stream);
        var frame = new LedFrame(83);
        for (int i = 0; i < frame.Length; i++) frame[i] = new Rgb((byte)i, 120, 200);
        var expected = new byte[device.Profile.FrameBufferSize];
        DirectProtocol.BuildFrame(device.Profile, frame.Pixels, expected);
        await device.WriteFrameAsync(frame, TestContext.Current.CancellationToken);
        var packets = stream.Writes.Where(r => r[1] == 0xC0).ToArray();
        Assert.Equal(7, packets.Length);
        Assert.Equal(new byte[] { 15, 15, 15, 15, 15, 15, 6 }, packets.Select(p => p[3]));
        var entries = packets.SelectMany(p => Enumerable.Range(0, p[3]).Select(i => p[(5 + i * 4)..(9 + i * 4)])).ToArray();
        var original = Enumerable.Range(0, 6).SelectMany(p => Enumerable.Range(0, expected[p * 65 + 3])
            .Select(i => expected[(p * 65 + 5 + i * 4)..(p * 65 + 9 + i * 4)])).ToArray();
        for (int i = 0; i < 83; i++) Assert.Equal(original[i], entries[i]);
        int[] sources = [0, 1, 2, 4, 5, 6, 7, 8, 9, 11, 12, 13, 14];
        for (int i = 0; i < 13; i++) Assert.Equal(new byte[] { (byte)(17 + 8 * i), (byte)sources[i], 120, 200 }, entries[83 + i]);
        Assert.Equal(83, device.Profile.LedCount);
        Assert.Equal(7, stream.DirectAcknowledgementsRead);
    }

    [Fact]
    public async Task ExplicitDecorativePixelsAreWrittenInsteadOfMirroredKeys()
    {
        var stream = new KeyboardStream();
        await using var device = new Falchion75Device(stream);
        var frame = new LedFrame(96);
        for(int i=0;i<83;i++)frame[i]=new Rgb(255,0,0);
        for(int i=83;i<96;i++)frame[i]=new Rgb(0,(byte)i,200);
        await device.WriteFrameAsync(frame,TestContext.Current.CancellationToken);
        var entries=stream.Writes.Where(p=>p[1]==0xC0).SelectMany(p=>Enumerable.Range(0,p[3]).Select(i=>p[(5+i*4)..(9+i*4)])).ToArray();
        for(int i=0;i<13;i++)Assert.Equal(new byte[]{(byte)(17+8*i),0,(byte)(83+i),200},entries[83+i]);
        Assert.Equal(7,stream.DirectAcknowledgementsRead);
    }

    [Fact]
    public async Task ReadInfoUsesVerifiedQueryWithoutChangingLightingMode()
    {
        var stream = new KeyboardStream();
        await using var device = new Falchion75Device(stream);
        var info = await device.ReadInfoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("1.00.50", info.FirmwareVersion);
        Assert.Equal(-1, info.LayoutId);
        Assert.Single(stream.Writes);
        Assert.Equal(0x12, stream.Writes[0][1]);
        Assert.Equal(0, stream.SoftwareMode);
    }

    [Fact]
    public async Task WrongAckCannotAdvanceTheFrameAndShutdownStillRestoresMode()
    {
        var stream = new KeyboardStream { WrongDirectCount = true };
        var device = new Falchion75Device(stream);
        await Assert.ThrowsAsync<TimeoutException>(() => device.WriteFrameAsync(new LedFrame(83), TestContext.Current.CancellationToken));
        Assert.Single(stream.Writes, r => r[1] == 0xC0);
        await device.DisposeAsync();
        Assert.Equal(0, stream.SoftwareMode);
        Assert.True(stream.Disposed);
    }

    [Fact]
    public async Task CancellationStopsSendingAndRestorationUsesItsOwnTimeout()
    {
        var stream = new KeyboardStream { WrongDirectCount = true };
        var device = new Falchion75Device(stream);
        using var cancellation = new CancellationTokenSource(80);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => device.WriteFrameAsync(new LedFrame(83), cancellation.Token));
        await device.DisposeAsync();
        Assert.Single(stream.Writes, r => r[1] == 0xC0);
        Assert.Equal(0, stream.SoftwareMode);
    }

    [Fact]
    public async Task ExistingSoftwareModeIsLeftAlone()
    {
        var stream = new KeyboardStream { SoftwareMode = 1 };
        var device = new Falchion75Device(stream);
        await Assert.ThrowsAsync<InvalidOperationException>(() => device.WriteFrameAsync(new LedFrame(83), TestContext.Current.CancellationToken));
        await device.DisposeAsync();
        Assert.Equal(1, stream.SoftwareMode);
        Assert.DoesNotContain(stream.Writes, r => r[1] == 0x74 || r[1] == 0xC0);
    }

    [Fact]
    public async Task FailedModeEntryStillAttemptsRestoration()
    {
        var stream = new KeyboardStream { FailEnable = true };
        var device = new Falchion75Device(stream);
        await Assert.ThrowsAsync<IOException>(() => device.WriteFrameAsync(new LedFrame(83), TestContext.Current.CancellationToken));
        await device.DisposeAsync();
        Assert.Contains(stream.Writes, r => r[1] == 0x74 && r[5] == 0);
        Assert.True(stream.Disposed);
    }

    [Fact]
    public async Task FlashAndUnverifiedPresetWritesAreRejectedWithoutIo()
    {
        var stream = new KeyboardStream();
        await using var device = new Falchion75Device(stream);
        await Assert.ThrowsAsync<NotSupportedException>(() => device.SaveToFlashAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<NotSupportedException>(() => device.ApplyHardwareEffectAsync(
            new HardwareEffect(HardwareEffectKind.Static, Rgb.White), TestContext.Current.CancellationToken));
        Assert.Empty(stream.Writes);
    }

    // A second Direct write before its matching reply is consumed is a failure.
    private sealed class KeyboardStream : Stream
    {
        private readonly Channel<byte[]> _replies = Channel.CreateUnbounded<byte[]>();
        private byte _waitingCount;
        public List<byte[]> Writes { get; } = new();
        public byte SoftwareMode { get; set; }
        public bool AddNoise { get; init; }
        public bool WrongDirectCount { get; init; }
        public bool FailEnable { get; init; }
        public bool Disposed { get; private set; }
        public int DirectAcknowledgementsRead { get; private set; }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            byte[] request = buffer.ToArray();
            Assert.Equal(65, request.Length);
            Writes.Add(request);
            byte[] reply = (byte[])request.Clone();
            switch (request[1])
            {
                case 0x12:
                    reply[5] = 0x50; reply[6] = 0; reply[7] = 1;
                    break;
                case 0x27:
                    reply[5] = SoftwareMode;
                    break;
                case 0x74:
                    SoftwareMode = request[5];
                    if (FailEnable && request[5] == 1) throw new IOException("Lost mode-entry acknowledgement.");
                    break;
                case 0xC0:
                    Assert.Equal(0, _waitingCount);
                    Assert.Equal(1, SoftwareMode);
                    _waitingCount = request[3];
                    reply[2] = 0x82;
                    if (WrongDirectCount) reply[3]--;
                    break;
                default:
                    throw new IOException("Unexpected command.");
            }
            if (AddNoise)
            {
                var noise = new byte[65]; noise[1] = 0x70;
                _replies.Writer.TryWrite(noise);
            }
            _replies.Writer.TryWrite(reply);
            return ValueTask.CompletedTask;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            var reply = await _replies.Reader.ReadAsync(ct);
            if (reply[1] == 0xC0 && reply[2] == 0x82 && reply[3] == _waitingCount)
            {
                _waitingCount = 0;
                DirectAcknowledgementsRead++;
            }
            reply.AsMemory().CopyTo(buffer);
            return reply.Length;
        }

        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        public override bool CanRead => !Disposed;
        public override bool CanWrite => !Disposed;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
