using System.Runtime.InteropServices;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Windows;

/// <summary>Opt-in, local-only primary display sampling; retains only a tiny RGB field.</summary>
public sealed partial class Win32ScreenColorSource : IScreenColorSource, IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stop = new();
    private Task? _worker;
    private volatile bool _active;
    private bool _disposed;
    private Rgb[] _colors = [];
    public int Width => 16;
    public int Height => 9;
    public ReadOnlyMemory<Rgb> Colors => Volatile.Read(ref _colors);

    public void SetActive(bool active)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _active = active;
            if (!active) Volatile.Write(ref _colors, []);
            else _worker ??= Task.Run(SampleAsync);
        }
    }

    private async Task SampleAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                if (_active)
                {
                    var colors = Capture();
                    lock (_gate)
                        if (_active && !_disposed && colors is not null) Volatile.Write(ref _colors, colors);
                }
                await Task.Delay(100, _stop.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    private Rgb[]? Capture()
    {
        nint screen = 0, memory = 0, bitmap = 0, previous = 0;
        try
        {
            screen = GetDC(0);
            if (screen == 0) return null;
            memory = CreateCompatibleDC(screen);
            if (memory == 0) return null;
            bitmap = CreateCompatibleBitmap(screen, Width, Height);
            if (bitmap == 0) return null;
            previous = SelectObject(memory, bitmap);
            if (previous == 0 || previous == -1) return null;
            SetStretchBltMode(memory, 4); // HALFTONE averages the scene while downsampling.
            int screenWidth = GetSystemMetrics(0), screenHeight = GetSystemMetrics(1);
            if (screenWidth <= 0 || screenHeight <= 0 || !_active) return null;
            if (StretchBlt(memory, 0, 0, Width, Height, screen, 0, 0, screenWidth, screenHeight, 0x00CC0020) == 0) return null;
            SelectObject(memory, previous);
            previous = 0;
            var info = new BitmapInfo { Size = 40, Width = Width, Height = -Height, Planes = 1, BitCount = 32 };
            var bytes = new byte[Width * Height * 4];
            if (GetDIBits(memory, bitmap, 0, (uint)Height, bytes, ref info, 0) != Height) return null;
            var result = new Rgb[Width * Height];
            for (int i = 0; i < result.Length; i++) result[i] = new Rgb(bytes[i * 4 + 2], bytes[i * 4 + 1], bytes[i * 4]);
            return result;
        }
        finally
        {
            if (previous != 0 && previous != -1) SelectObject(memory, previous);
            if (bitmap != 0) DeleteObject(bitmap);
            if (memory != 0) DeleteDC(memory);
            if (screen != 0) ReleaseDC(0, screen);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task? worker;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _active = false;
            Volatile.Write(ref _colors, []);
            _stop.Cancel();
            worker = _worker;
        }
        // Native display drivers can stall; never hold application shutdown indefinitely.
        if (worker is not null)
        {
            try { await worker.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
            catch (TimeoutException)
            {
                _ = worker.ContinueWith(_ => _stop.Dispose(), TaskScheduler.Default);
                return;
            }
        }
        _stop.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, BitCount;
        public uint Compression, SizeImage;
        public int XPelsPerMeter, YPelsPerMeter;
        public uint ClrUsed, ClrImportant;
        public uint Color;
    }

    [LibraryImport("user32.dll")] private static partial nint GetDC(nint window);
    [LibraryImport("user32.dll")] private static partial int ReleaseDC(nint window, nint dc);
    [LibraryImport("user32.dll")] private static partial int GetSystemMetrics(int index);
    [LibraryImport("gdi32.dll")] private static partial nint CreateCompatibleDC(nint dc);
    [LibraryImport("gdi32.dll")] private static partial nint CreateCompatibleBitmap(nint dc, int width, int height);
    [LibraryImport("gdi32.dll")] private static partial nint SelectObject(nint dc, nint obj);
    [LibraryImport("gdi32.dll")] private static partial int DeleteObject(nint obj);
    [LibraryImport("gdi32.dll")] private static partial int DeleteDC(nint dc);
    [LibraryImport("gdi32.dll")] private static partial int SetStretchBltMode(nint dc, int mode);
    [LibraryImport("gdi32.dll")] private static partial int StretchBlt(nint dest, int x, int y, int width, int height, nint source, int sourceX, int sourceY, int sourceWidth, int sourceHeight, uint operation);
    [LibraryImport("gdi32.dll")] private static partial int GetDIBits(nint dc, nint bitmap, uint start, uint lines, [Out] byte[] bits, ref BitmapInfo info, uint usage);
}
