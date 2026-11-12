using RSS_II_RGB.App;
using System.Diagnostics;
namespace ROG_KB_RGB_Studio.App.Tests;
public class GifHelperTimeoutTests
{
    [Fact] public async Task AStalledHelperIsTerminatedAtTheDeadline()
    {
        var start=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true};
        start.ArgumentList.Add("-NoProfile");start.ArgumentList.Add("-Command");start.ArgumentList.Add("Start-Sleep -Seconds 20");
        using var child=Process.Start(start)!;
        await Assert.ThrowsAsync<TimeoutException>(()=>GifImportService.WaitForHelperExitAsync(child,TimeSpan.FromMilliseconds(100)));
        Assert.True(child.HasExited);
    }
    [Fact] public async Task ACompletedHelperFinishesWithoutWaitingForTheDeadline()
    {
        using var child=Process.Start(new ProcessStartInfo("cmd.exe","/c exit 0"){UseShellExecute=false,CreateNoWindow=true})!;
        await GifImportService.WaitForHelperExitAsync(child,TimeSpan.FromSeconds(5));Assert.Equal(0,child.ExitCode);
    }
}
