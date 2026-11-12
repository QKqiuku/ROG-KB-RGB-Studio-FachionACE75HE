using System.Runtime.InteropServices;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;

namespace RSS_II_RGB.SensorsHost;

/// <summary>Windows application-loopback activation, including descendants. No endpoint rerouting.</summary>
public sealed class ProcessLoopbackCapture : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _worker;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Ready => _ready.Task;
    public Exception? Error { get; private set; }
    public bool IsCapturing { get; private set; }
    public ProcessLoopbackCapture(int pid, long startTimeTicks, Action<float[], int, int> samples)
    {
        _worker = Task.Run(async () =>
        {
            AudioClient? client = null;
            try
            {
                using var target = Process.GetProcessById(pid);
                _ = target.Handle; // Retain the process object throughout activation and capture.
                if (target.HasExited || target.StartTime.ToUniversalTime().Ticks != startTimeTicks) throw new InvalidOperationException("The selected process has exited or changed.");
                IntPtr parameters = Marshal.AllocCoTaskMem(12);
                var callback = new Completion(parameters);
                callback.AssertAgile();
                Marshal.WriteInt32(parameters, 0, 1); Marshal.WriteInt32(parameters, 4, pid); Marshal.WriteInt32(parameters, 8, 0);
                var variant = new ActivationVariant { Type = 65, Size = 12, Data = parameters };
                Guid iid = typeof(IAudioClient).GUID;
                int activation;
                IOperation operation;
                try { activation = ActivateAudioInterfaceAsync("VAD\\Process_Loopback", ref iid, ref variant, callback, out operation); }
                catch { callback.ReleaseParameters(); throw; }
                if (activation < 0) { callback.ReleaseParameters(); Marshal.ThrowExceptionForHR(activation); }
                // Windows retains activation memory until completion, even when our wait times out.
                try { client = new AudioClient((IAudioClient)await callback.Result.Task.WaitAsync(TimeSpan.FromSeconds(5), _stop.Token)); }
                finally { Marshal.ReleaseComObject(operation); }
                var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
                client.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.Loopback | AudioClientStreamFlags.EventCallback | (AudioClientStreamFlags)0x80000000, 0, 0, format, Guid.Empty);
                using var ready = new AutoResetEvent(false);
                client.SetEventHandle(ready.SafeWaitHandle.DangerousGetHandle());
                var capture = client.AudioCaptureClient;
                if (target.HasExited) throw new InvalidOperationException("The selected process has exited.");
                client.Start();
                IsCapturing = true;
                _ready.TrySetResult();
                while (!_stop.IsCancellationRequested)
                {
                    ready.WaitOne(20);
                    while (capture.GetNextPacketSize() > 0)
                    {
                        IntPtr data = capture.GetBuffer(out int frames, out AudioClientBufferFlags flags, out _, out _);
                        try
                        {
                            if (frames < 0 || frames > 480000) throw new InvalidDataException("Invalid audio packet size.");
                            var pcm = new float[frames * 2];
                            if ((flags & AudioClientBufferFlags.Silent) == 0) Marshal.Copy(data, pcm, 0, pcm.Length);
                            samples(pcm, 2, 48000);
                        }
                        finally { capture.ReleaseBuffer(frames); }
                    }
                }
                client.Stop();
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { _ready.TrySetCanceled(); }
            catch (Exception ex) { Error = ex; _ready.TrySetException(ex); }
            finally { IsCapturing = false; client?.Dispose(); }
        });
    }
    public void Dispose() { _stop.Cancel(); _worker.GetAwaiter().GetResult(); _stop.Dispose(); }
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct ActivationVariant { [FieldOffset(0)] public ushort Type; [FieldOffset(8)] public int Size; [FieldOffset(16)] public IntPtr Data; }
    [ComImport, Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOperation { [PreserveSig] int GetActivateResult(out int result, [MarshalAs(UnmanagedType.IUnknown)] out object client); }
    // CCWs hide non-public interfaces even with ComVisible: both interfaces and declaring type must be public.
    [ComVisible(true), Guid("41D949AB-9862-444A-80F6-C261334DA5EB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ICompletion { [PreserveSig] int ActivateCompleted(IOperation operation); }
    [ComVisible(true), Guid("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAgileObject { }
    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public sealed class Completion : ICompletion, IAgileObject
    {
        private IntPtr _parameters;
        public Completion(IntPtr parameters) => _parameters = parameters;
        public void AssertAgile()
        {
            IntPtr unknown = Marshal.GetIUnknownForObject(this);
            try
            {
                Guid agile = typeof(IAgileObject).GUID;
                int hr = Marshal.QueryInterface(unknown, in agile, out IntPtr pointer);
                if (hr < 0) { ReleaseParameters(); throw new InvalidOperationException($"Audio completion callback must expose IAgileObject (0x{hr:X8})."); }
                Marshal.Release(pointer);
            }
            finally { Marshal.Release(unknown); }
        }
        public void ReleaseParameters() { IntPtr p = Interlocked.Exchange(ref _parameters, IntPtr.Zero); if (p != IntPtr.Zero) Marshal.FreeCoTaskMem(p); }
        public readonly TaskCompletionSource<object> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int ActivateCompleted(IOperation operation)
        {
            try { Marshal.ThrowExceptionForHR(operation.GetActivateResult(out int hr, out object client)); Marshal.ThrowExceptionForHR(hr); Result.TrySetResult(client); }
            catch (Exception ex) { Result.TrySetException(ex); }
            finally { ReleaseParameters(); }
            return 0;
        }
    }
    [DllImport("Mmdevapi.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int ActivateAudioInterfaceAsync(string path, ref Guid iid, ref ActivationVariant parameters, ICompletion completion, out IOperation operation);
}
