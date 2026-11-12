namespace RSS_II_RGB.Core.Device;

public enum KeyboardConnectionIssue { Unavailable, SoftwareModeConflict }

public sealed class KeyboardConnectionException(KeyboardConnectionIssue issue, string message, Exception? inner = null)
    : IOException(message, inner)
{
    public KeyboardConnectionIssue Issue { get; } = issue;
}
