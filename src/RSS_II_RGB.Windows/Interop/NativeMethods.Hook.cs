using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
namespace RSS_II_RGB.Windows.Interop;

internal static partial class NativeMethods
{
    public const uint WM_INPUT=0x00ff,WM_QUIT=0x0012;
    public const uint WM_KEYDOWN=0x0100,WM_KEYUP=0x0101,WM_SYSKEYDOWN=0x0104,WM_SYSKEYUP=0x0105;
    public const uint RIDEV_INPUTSINK=0x100,RIDEV_REMOVE=1,RID_INPUT=0x10000003,RIM_TYPEKEYBOARD=1;
    public const ushort RI_KEY_BREAK=1,RI_KEY_E0=2,RI_KEY_E1=4;
    public static readonly nint HWND_MESSAGE=-3;
    [StructLayout(LayoutKind.Sequential)] public struct RAWINPUTDEVICE {public ushort usUsagePage,usUsage;public uint dwFlags;public nint hwndTarget;}
    [StructLayout(LayoutKind.Sequential)] public struct RAWINPUTHEADER {public uint dwType,dwSize;public nint hDevice;public nuint wParam;}
    [StructLayout(LayoutKind.Sequential)] public struct RAWKEYBOARD {public ushort MakeCode,Flags,Reserved,VKey;public uint Message,ExtraInformation;}
    [StructLayout(LayoutKind.Sequential)] public struct WNDCLASS
    {
        public uint style;public nint lpfnWndProc;public int cbClsExtra,cbWndExtra;
        public nint hInstance,hIcon,hCursor,hbrBackground,lpszMenuName,lpszClassName;
    }
    [StructLayout(LayoutKind.Sequential)] public struct MSG
    {public nint hwnd;public uint message;public nuint wParam;public nint lParam;public uint time;public int ptX,ptY;public uint lPrivate;}

    [LibraryImport("user32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterRawInputDevices(in RAWINPUTDEVICE device,uint count,uint size);
    [LibraryImport("user32.dll",SetLastError=true)]
    public static partial uint GetRawInputData(nint input,uint command,nint data,ref uint size,uint headerSize);
    [LibraryImport("user32.dll",SetLastError=true)]
    public static partial uint GetRegisteredRawInputDevices([Out,MarshalUsing(CountElementName=nameof(count))] RAWINPUTDEVICE[]? devices,ref uint count,uint size);
    [LibraryImport("user32.dll",SetLastError=true)]
    public static partial ushort RegisterClassW(in WNDCLASS definition);
    [LibraryImport("user32.dll",StringMarshalling=StringMarshalling.Utf16,SetLastError=true)]
    public static partial nint CreateWindowExW(uint extendedStyle,string className,string name,uint style,int x,int y,int width,int height,nint parent,nint menu,nint instance,nint parameter);
    [LibraryImport("user32.dll",StringMarshalling=StringMarshalling.Utf16,SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterClassW(string name,nint instance);
    [LibraryImport("user32.dll")]
    public static partial nint DefWindowProcW(nint window,uint message,nuint wParam,nint lParam);
    [LibraryImport("user32.dll")]
    [return:MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(nint window);
    [LibraryImport("user32.dll")]
    public static partial int GetMessageW(out MSG message,nint window,uint min,uint max);
    [LibraryImport("user32.dll")]
    public static partial nint DispatchMessageW(in MSG message);
    [LibraryImport("user32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostThreadMessageW(uint thread,uint message,nuint wParam,nint lParam);
    [LibraryImport("kernel32.dll",SetLastError=true,StringMarshalling=StringMarshalling.Utf16)]
    public static partial nint GetModuleHandleW(string? name);
    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();
}
