using RSS_II_RGB.App;
using RSS_II_RGB.Core.Device;

namespace ROG_KB_RGB_Studio.App.Tests;

public class ConnectionMonitorTests
{
    [Fact]
    public void ConflictAndOpenFailuresDoNotLookLikeMissingDevices()
    {
        Assert.Equal(L.StatusSearching, MainViewModel.ConnectionFailureText(null));
        Assert.Equal(L.ConnectionConflict, MainViewModel.ConnectionFailureText(new KeyboardConnectionException(KeyboardConnectionIssue.SoftwareModeConflict, "busy")));
        Assert.Equal(L.ConnectionUnavailable, MainViewModel.ConnectionFailureText(new KeyboardConnectionException(KeyboardConnectionIssue.Unavailable, "denied")));
        Assert.Equal(L.ConnectionFailed, MainViewModel.ConnectionFailureText(new IOException("timeout")));
    }
}
