namespace RSS_II_RGB.Core.Layout;

public static class KeyboardProfiles
{
    public const ushort Falchion75ProductId = 0x1C7D;

    public static KeyboardProfile? ForProduct(ushort productId, bool enableFalchion75 = false) => productId switch
    {
        0x1AB5 or 0x1AB3 => ScopeIILayout.Profile,
        Falchion75ProductId when enableFalchion75 => Falchion75Layout.Profile,
        _ => null,
    };
}
