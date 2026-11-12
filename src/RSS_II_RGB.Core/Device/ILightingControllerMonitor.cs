namespace RSS_II_RGB.Core.Device;

public interface ILightingControllerMonitor
{
    IReadOnlyList<string> GetRunningControllers();
}
