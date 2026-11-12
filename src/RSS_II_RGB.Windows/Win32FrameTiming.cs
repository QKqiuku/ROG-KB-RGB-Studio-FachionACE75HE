using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Windows.Interop;

namespace RSS_II_RGB.Windows;

/// <summary>Process timer precision lease, paired even when the render loop fails.</summary>
public sealed class Win32FrameTiming : IFrameTiming
{
    public IDisposable Enter() => new Lease();

    private sealed class Lease : IDisposable
    {
        private bool _active = NativeMethods.TimeBeginPeriod(1) == 0;
        public void Dispose()
        {
            if (!_active) return;
            NativeMethods.TimeEndPeriod(1);
            _active = false;
        }
    }
}
