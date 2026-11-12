using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RSS_II_RGB.Core.Input;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Windows.Interop;

namespace RSS_II_RGB.Windows;

/// <summary>Physical-key events via a background Raw Input sink, independent of foreground hook chains.</summary>
public sealed class Win32KeyboardHook : IKeyboardHook
{
    private static Win32KeyboardHook? _instance;
    private readonly KeyboardProfile _profile;
    private readonly ScancodeResolver _resolver;
    private readonly SemaphoreSlim _lifecycle=new(1,1);
    private TaskCompletionSource? _started;
    private Thread? _thread;
    private uint _threadId;
    private volatile bool _registered;
    private volatile bool _stopping;
    private bool _disposed;
    public Win32KeyboardHook(KeyboardProfile profile) {_profile=profile;_resolver=new(profile);}
    public event Action<KeyEvent>? KeyChanged;
    public bool IsInstalled=>_registered;
    private long _packetsReceived,_eventsDecoded,_packetReadFailures;
    internal long PacketsReceived=>Interlocked.Read(ref _packetsReceived);
    internal long EventsDecoded=>Interlocked.Read(ref _eventsDecoded);
    internal long PacketReadFailures=>Interlocked.Read(ref _packetReadFailures);

    public async Task StartAsync(CancellationToken ct=default)
    {
        await _lifecycle.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed,this);
            if(_thread is not null)return;
            if(Interlocked.CompareExchange(ref _instance,this,null) is not null)
                throw new InvalidOperationException("A keyboard input receiver is already active.");
            _stopping=false;_started=new(TaskCreationOptions.RunContinuationsAsynchronously);
            _thread=new Thread(MessageLoop) {IsBackground=true,Name=WindowsConstants.KeyboardInputThreadName};
            _thread.Start();
            try {await _started.Task.WaitAsync(ct).ConfigureAwait(false);}
            catch {await StopCoreAsync().ConfigureAwait(false);throw;}
        }
        finally {_lifecycle.Release();}
    }
    public async Task StopAsync(CancellationToken ct=default)
    {
        await _lifecycle.WaitAsync(ct).ConfigureAwait(false);
        try {await StopCoreAsync().ConfigureAwait(false);}
        finally {_lifecycle.Release();}
    }
    private async Task StopCoreAsync()
    {
        _stopping=true;
        if(_threadId!=0)NativeMethods.PostThreadMessageW(_threadId,NativeMethods.WM_QUIT,0,0);
        if(_thread is { } thread)await Task.Run(()=>thread.Join()).ConfigureAwait(false);
        _thread=null;_started=null;
    }
    public async ValueTask DisposeAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try {_disposed=true;await StopCoreAsync().ConfigureAwait(false);}
        finally {_lifecycle.Release();}
    }
    private unsafe void MessageLoop()
    {
        nint window=0;ushort atom=0;nint name=Marshal.StringToHGlobalUni(WindowsConstants.KeyboardInputWindowClass);
        nint module=NativeMethods.GetModuleHandleW(null);
        try
        {
            _threadId=NativeMethods.GetCurrentThreadId();
            var definition=new NativeMethods.WNDCLASS {hInstance=module,lpszClassName=name,
                lpfnWndProc=(nint)(delegate* unmanaged[Stdcall]<nint,uint,nuint,nint,nint>)&WindowProc};
            atom=NativeMethods.RegisterClassW(in definition);
            if(atom==0)throw new Win32Exception(Marshal.GetLastPInvokeError());
            window=NativeMethods.CreateWindowExW(0,WindowsConstants.KeyboardInputWindowClass,"",0,0,0,0,0,NativeMethods.HWND_MESSAGE,0,module,0);
            if(window==0)throw new Win32Exception(Marshal.GetLastPInvokeError());
            var registration=new NativeMethods.RAWINPUTDEVICE {usUsagePage=1,usUsage=6,dwFlags=NativeMethods.RIDEV_INPUTSINK,hwndTarget=window};
            // INPUTSINK receives background input; never use NOLEGACY, NOHOTKEYS or a consuming hook.
            if(!NativeMethods.RegisterRawInputDevices(in registration,1,(uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>()))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            _registered=true;_started!.TrySetResult();
            while(!_stopping && NativeMethods.GetMessageW(out var message,0,0,0)>0)NativeMethods.DispatchMessageW(in message);
        }
        catch(Exception ex) {_started?.TrySetException(ex);}
        finally
        {
            _registered=false;
            if(window!=0)
            {
                var remove=new NativeMethods.RAWINPUTDEVICE {usUsagePage=1,usUsage=6,dwFlags=NativeMethods.RIDEV_REMOVE};
                NativeMethods.RegisterRawInputDevices(in remove,1,(uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>());
                NativeMethods.DestroyWindow(window);
            }
            if(atom!=0)NativeMethods.UnregisterClassW(WindowsConstants.KeyboardInputWindowClass,module);
            Marshal.FreeHGlobal(name);_threadId=0;
            Interlocked.CompareExchange(ref _instance,null,this);
        }
    }
    [UnmanagedCallersOnly(CallConvs=[typeof(CallConvStdcall)])]
    private static nint WindowProc(nint window,uint message,nuint wParam,nint lParam)
    {
        if(message==NativeMethods.WM_INPUT)
        {
            // Exceptions must not escape an unmanaged window procedure.
            try {_instance?.ReadPacket(lParam);}catch { }
        }
        // DefWindowProc also releases foreground WM_INPUT resources.
        return NativeMethods.DefWindowProcW(window,message,wParam,lParam);
    }
    private unsafe void ReadPacket(nint handle)
    {
        Interlocked.Increment(ref _packetsReceived);
        Span<byte> packet=stackalloc byte[64];uint size=(uint)packet.Length;
        fixed(byte* buffer=packet)
        {
            uint read=NativeMethods.GetRawInputData(handle,NativeMethods.RID_INPUT,(nint)buffer,ref size,(uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>());
            if(read==uint.MaxValue || read>packet.Length){Interlocked.Increment(ref _packetReadFailures);return;}
            if(TryDecode(packet[..(int)read],Environment.TickCount64,out var key))
            {Interlocked.Increment(ref _eventsDecoded);KeyChanged?.Invoke(key);}
        }
    }
    internal bool TryDecode(ReadOnlySpan<byte> packet,long timestamp,out KeyEvent key)
    {
        key=default;int headerSize=Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>();
        int required=headerSize+Marshal.SizeOf<NativeMethods.RAWKEYBOARD>();
        if(packet.Length<required)return false;
        var header=MemoryMarshal.Read<NativeMethods.RAWINPUTHEADER>(packet);
        if(header.dwType!=NativeMethods.RIM_TYPEKEYBOARD || header.dwSize<required || header.dwSize>packet.Length)return false;
        var keyboard=MemoryMarshal.Read<NativeMethods.RAWKEYBOARD>(packet[headerSize..]);
        if(keyboard.MakeCode is 0 or 0xff || keyboard.VKey==0xff || (keyboard.Flags&NativeMethods.RI_KEY_E1)!=0)return false;
        bool down=(keyboard.Flags&NativeMethods.RI_KEY_BREAK)==0;
        if(keyboard.Message is not (NativeMethods.WM_KEYDOWN or NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYDOWN or NativeMethods.WM_SYSKEYUP))return false;
        int index=_resolver.ToKeyIndex(keyboard.MakeCode,(keyboard.Flags&NativeMethods.RI_KEY_E0)!=0);
        if(index<0)return false;
        key=new(index,_profile.ByIndex(index).KeyId,down,timestamp);return true;
    }
}
