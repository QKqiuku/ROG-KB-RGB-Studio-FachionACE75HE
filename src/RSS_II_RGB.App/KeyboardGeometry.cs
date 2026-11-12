using RSS_II_RGB.Core.Layout;

namespace RSS_II_RGB.App;

internal readonly record struct KeyGeometry(double X, double Y, double Width, double Height);

/// <summary>Visual key units; deliberately separate from the effect/LED matrix.</summary>
internal static class KeyboardGeometry
{
    public const double Unit = 40;
    public const double Gap = 5;

    private static readonly double[][] FalchionX =
    {
        new double[] { 0, 1.25, 2.25, 3.25, 4.25, 5.5, 6.5, 7.5, 8.5, 9.75, 10.75, 11.75, 12.75, 14, 15 },
        new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15 },
        new double[] { 0, 1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5, 8.5, 9.5, 10.5, 11.5, 12.5, 13.5, 15 },
        new double[] { 0, 1.75, 2.75, 3.75, 4.75, 5.75, 6.75, 7.75, 8.75, 9.75, 10.75, 11.75, 12.75, 15 },
        new double[] { 0, 2.25, 3.25, 4.25, 5.25, 6.25, 7.25, 8.25, 9.25, 10.25, 11.25, 12.25, 14, 15 },
        new double[] { 0, 1.25, 2.5, 3.75, 10, 11, 12, 13, 14, 15 },
    };

    public static double Width(KeyboardProfile profile) =>
        ReferenceEquals(profile, Falchion75Layout.Profile) ? 16 * Unit : profile.Cols * Unit;
    public static double Height(KeyboardProfile profile) =>
        ReferenceEquals(profile, Falchion75Layout.Profile) ? 6.25 * Unit : profile.Rows * Unit;

    public static KeyGeometry ForKey(KeyboardProfile profile, LedKey key)
    {
        if (!ReferenceEquals(profile, Falchion75Layout.Profile))
            return new(key.Col * Unit, key.Row * Unit, Unit - Gap, Unit - Gap);

        int position = 0;
        foreach (var previous in profile.Keys)
        {
            if (previous.Index == key.Index) break;
            if (previous.Row == key.Row) position++;
        }
        double width = key.Name switch
        {
            "TAB" or "ANSI_BACK_SLASH" => 1.5,
            "CAPS_LOCK" or "RIGHT_SHIFT" => 1.75,
            "LEFT_SHIFT" or "ANSI_ENTER" => 2.25,
            "BACKSPACE" => 2,
            "LEFT_CONTROL" or "LEFT_WINDOWS" or "LEFT_ALT" => 1.25,
            "SPACE" => 6.25,
            _ => 1,
        };
        double y = key.Row == 0 ? 0 : key.Row + 0.25;
        return new(FalchionX[key.Row][position] * Unit, y * Unit, width * Unit - Gap, Unit - Gap);
    }
}
