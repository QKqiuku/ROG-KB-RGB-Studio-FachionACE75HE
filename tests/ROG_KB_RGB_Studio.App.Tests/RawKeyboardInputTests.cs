using RSS_II_RGB.App;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using RSS_II_RGB.Core.Input;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Windows;
using RSS_II_RGB.Windows.Interop;
namespace ROG_KB_RGB_Studio.App.Tests;
public class RawKeyboardInputTests
{
    [Theory]
    [InlineData(0x1e,0,0x100,"A",true)]
    [InlineData(0x1e,1,0x101,"A",false)]
    [InlineData(0x48,2,0x100,"UP_ARROW",true)]
    [InlineData(0x48,3,0x101,"UP_ARROW",false)]
    [InlineData(0x36,0,0x100,"RIGHT_SHIFT",true)]
    [InlineData(0x38,2,0x104,"RIGHT_ALT",true)]
    [InlineData(0x38,3,0x105,"RIGHT_ALT",false)]
    public async Task PhysicalPacketsPreservePositionsAndTransitions(int scan,int flags,int message,string key,bool down)
    {
        await using var input=new Win32KeyboardHook(Falchion75Layout.Profile);
        Assert.True(input.TryDecode(Packet(scan,flags,message),123,out var e));
        Assert.Equal(Falchion75Layout.Profile.IndexForKeyName(key),e.KeyIndex);Assert.Equal(down,e.IsDown);Assert.Equal(123,e.TimestampMs);
        Assert.Equal(Falchion75Layout.Profile.ByIndex(e.KeyIndex).KeyId,e.KeyId);
    }
    [Fact]
    public async Task InvalidAndNonKeyboardPacketsCannotProducePhantomKeys()
    {
        await using var input=new Win32KeyboardHook(Falchion75Layout.Profile);
        var mouse=Packet(0x1e,0,0x100);BinaryPrimitives.WriteUInt32LittleEndian(mouse,0);
        var tooShort=Packet(0x1e,0,0x100);BinaryPrimitives.WriteUInt32LittleEndian(tooShort.AsSpan(4),1);
        foreach(var packet in new[]{mouse,tooShort,new byte[3],Packet(0xff,0,0x100),Packet(0x1d,4,0x100),Packet(0,0,0x100),Packet(0x1e,0,0)})
            Assert.False(input.TryDecode(packet,0,out _));
        Assert.Equal(24,Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>());Assert.Equal(16,Marshal.SizeOf<NativeMethods.RAWKEYBOARD>());
    }
    [Fact]
    public async Task ReceiverRegistersInBackgroundWithoutChangingLegacyInputAndCanRestart()
    {
        await using var input=new Win32KeyboardHook(Falchion75Layout.Profile);
        for(int i=0;i<2;i++)
        {
            await input.StartAsync(TestContext.Current.CancellationToken);Assert.True(input.IsInstalled);
            await input.StartAsync(TestContext.Current.CancellationToken);
            await using(var duplicate=new Win32KeyboardHook(Falchion75Layout.Profile))
                await Assert.ThrowsAsync<InvalidOperationException>(()=>duplicate.StartAsync(TestContext.Current.CancellationToken));
            Assert.True(input.IsInstalled);
            uint count=0;uint size=(uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>();
            Assert.Equal(0u,NativeMethods.GetRegisteredRawInputDevices(null,ref count,size));
            var registrations=new NativeMethods.RAWINPUTDEVICE[count];Assert.NotEqual(uint.MaxValue,NativeMethods.GetRegisteredRawInputDevices(registrations,ref count,size));
            var keyboard=Assert.Single(registrations,r=>r.usUsagePage==1 && r.usUsage==6);
            Assert.Equal(0x100u,keyboard.dwFlags);Assert.NotEqual(0,keyboard.hwndTarget);
            await input.StopAsync(TestContext.Current.CancellationToken);Assert.False(input.IsInstalled);
            count=0;Assert.Equal(0u,NativeMethods.GetRegisteredRawInputDevices(null,ref count,size));
            var remaining=new NativeMethods.RAWINPUTDEVICE[count];NativeMethods.GetRegisteredRawInputDevices(remaining,ref count,size);
            Assert.DoesNotContain(remaining,r=>r.usUsagePage==1 && r.usUsage==6);
        }
        await input.StopAsync(TestContext.Current.CancellationToken);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>input.StartAsync(cancelled.Token));Assert.False(input.IsInstalled);
    }
    private static byte[] Packet(int scan,int flags,int message)
    {
        var p=new byte[40];BinaryPrimitives.WriteUInt32LittleEndian(p,1);BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(4),40);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(24),(ushort)scan);BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(26),(ushort)flags);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(30),0x41);BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(32),(uint)message);return p;
    }
}
