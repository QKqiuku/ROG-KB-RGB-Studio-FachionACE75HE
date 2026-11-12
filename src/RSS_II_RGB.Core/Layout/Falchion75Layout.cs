namespace RSS_II_RGB.Core.Layout;

/// <summary>
/// ANSI 83-key layout based on Gear Link 1.00.24, with measured address corrections.
/// The key profile excludes decorative LEDs; the device appends their mirrored colours.
/// </summary>
public static class Falchion75Layout
{
    public const int LightBarLedCount = 13;
    public const int DirectPacketsPerFrame = 7;
    // Measured left-to-right strip positions (12), then the ROG eye.
    public static ReadOnlySpan<byte> LightBarIds => [17, 25, 33, 41, 49, 57, 65, 73, 81, 89, 97, 105, 113];
    public static ReadOnlySpan<int> LightBarSourceIndices => [0, 1, 2, 4, 5, 6, 7, 8, 9, 11, 12, 13, 14];

    // shortcut: full-key colour labelling and sleep/reconnect are not yet validated; keep explicit opt-in.
    public static KeyboardProfile Profile { get; } = new(
        "ROG Falchion Ace 75 HE (experimental)", 6, 15, new LedKey[]
        {
            new(0x02, "ESCAPE", 0, 0, 0),
            new(0x0A, "F1", 0, 1, 1),
            new(0x12, "F2", 0, 2, 2),
            new(0x1A, "F3", 0, 3, 3),
            new(0x22, "F4", 0, 4, 4),
            new(0x2A, "F5", 0, 5, 5),
            new(0x32, "F6", 0, 6, 6),
            new(0x3A, "F7", 0, 7, 7),
            new(0x42, "F8", 0, 8, 8),
            new(0x4A, "F9", 0, 9, 9),
            new(0x52, "F10", 0, 10, 10),
            new(0x5A, "F11", 0, 11, 11),
            new(0x62, "F12", 0, 12, 12),
            new(0x6A, "HOME", 0, 13, 13),
            new(0x72, "END", 0, 14, 14),
            new(0x03, "BACK_TICK", 1, 0, 15),
            new(0x0B, "1", 1, 1, 16),
            new(0x13, "2", 1, 2, 17),
            new(0x1B, "3", 1, 3, 18),
            new(0x23, "4", 1, 4, 19),
            new(0x2B, "5", 1, 5, 20),
            new(0x33, "6", 1, 6, 21),
            new(0x3B, "7", 1, 7, 22),
            new(0x43, "8", 1, 8, 23),
            new(0x4B, "9", 1, 9, 24),
            new(0x53, "0", 1, 10, 25),
            new(0x5B, "MINUS", 1, 11, 26),
            new(0x63, "EQUALS", 1, 12, 27),
            new(0x6B, "BACKSPACE", 1, 13, 28),
            new(0x73, "INSERT", 1, 14, 29),
            new(0x04, "TAB", 2, 0, 30),
            new(0x0C, "Q", 2, 1, 31),
            new(0x14, "W", 2, 2, 32),
            new(0x1C, "E", 2, 3, 33),
            new(0x24, "R", 2, 4, 34),
            new(0x2C, "T", 2, 5, 35),
            new(0x34, "Y", 2, 6, 36),
            new(0x3C, "U", 2, 7, 37),
            new(0x44, "I", 2, 8, 38),
            new(0x4C, "O", 2, 9, 39),
            new(0x54, "P", 2, 10, 40),
            new(0x5C, "LEFT_BRACKET", 2, 11, 41),
            new(0x64, "RIGHT_BRACKET", 2, 12, 42),
            new(0x6C, "ANSI_BACK_SLASH", 2, 13, 43),
            new(0x74, "DELETE", 2, 14, 44),
            new(0x05, "CAPS_LOCK", 3, 0, 45),
            new(0x0D, "A", 3, 1, 46),
            new(0x15, "S", 3, 2, 47),
            new(0x1D, "D", 3, 3, 48),
            new(0x25, "F", 3, 4, 49),
            new(0x2D, "G", 3, 5, 50),
            new(0x35, "H", 3, 6, 51),
            new(0x3D, "J", 3, 7, 52),
            new(0x45, "K", 3, 8, 53),
            new(0x4D, "L", 3, 9, 54),
            new(0x55, "SEMICOLON", 3, 10, 55),
            new(0x5D, "QUOTE", 3, 11, 56),
            new(0x6D, "ANSI_ENTER", 3, 12, 57),
            new(0x75, "PAGE_UP", 3, 14, 58),
            new(0x06, "LEFT_SHIFT", 4, 0, 59),
            new(0x16, "Z", 4, 2, 60),
            new(0x1E, "X", 4, 3, 61),
            new(0x26, "C", 4, 4, 62),
            new(0x2E, "V", 4, 5, 63),
            new(0x36, "B", 4, 6, 64),
            new(0x3E, "N", 4, 7, 65),
            new(0x46, "M", 4, 8, 66),
            new(0x4E, "COMMA", 4, 9, 67),
            new(0x56, "PERIOD", 4, 10, 68),
            new(0x5E, "FORWARD_SLASH", 4, 11, 69),
            new(0x66, "RIGHT_SHIFT", 4, 12, 70),
            new(0x6E, "UP_ARROW", 4, 13, 71),
            new(0x76, "PAGE_DOWN", 4, 14, 72),
            new(0x07, "LEFT_CONTROL", 5, 0, 73),
            new(0x0F, "LEFT_WINDOWS", 5, 1, 74),
            new(0x17, "LEFT_ALT", 5, 2, 75),
            new(0x37, "SPACE", 5, 6, 76),
            new(0x4F, "RIGHT_ALT", 5, 9, 77),
            new(0x57, "RIGHT_FUNCTION", 5, 10, 78),
            new(0x5F, "RIGHT_CONTROL", 5, 11, 79),
            new(0x67, "LEFT_ARROW", 5, 12, 80),
            new(0x6F, "DOWN_ARROW", 5, 13, 81),
            new(0x77, "RIGHT_ARROW", 5, 14, 82),
        }, KeyboardFormFactor.Compact75);
    public static KeyboardProfile SpatialProfile { get; } = CreateSpatialProfile();

    private static KeyboardProfile CreateSpatialProfile()
    {
        var keys = Profile.Keys.ToArray().ToList();
        for (int i = 0; i < LightBarLedCount; i++)
            keys.Add(new LedKey(LightBarIds[i], i == 12 ? "ROG_EYE" : $"ROG_BAR_{i + 1}",
                -1, LightBarSourceIndices[i], Profile.LedCount + i));
        return new KeyboardProfile(Profile.Name + " lighting positions", Profile.Rows, Profile.Cols, keys.ToArray(), Profile.FormFactor);
    }
}
