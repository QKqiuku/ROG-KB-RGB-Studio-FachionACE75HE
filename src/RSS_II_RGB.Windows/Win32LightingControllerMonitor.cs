using System.Diagnostics;
using RSS_II_RGB.Core.Device;

namespace RSS_II_RGB.Windows;

public sealed class Win32LightingControllerMonitor : ILightingControllerMonitor
{
    public IReadOnlyList<string> GetRunningControllers()
    {
        string[] known = ["LightingService", "ArmouryCrate", "ArmourySwAgent", "ArmourySocketServer", "OpenRGB", "SignalRgb", "Lumo"];
        var found = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try { if (known.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase)) found.Add(process.ProcessName); }
                catch (System.ComponentModel.Win32Exception) { }
                catch (InvalidOperationException) { }
            }
        }
        return found.ToArray();
    }
}
