using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;
namespace RSS_II_RGB.Core.Engine;

public enum DecorationMode { FollowKeys, Spatial, Custom }

/// <summary>Composes decorative positions separately so their modes cannot change key pixels.</summary>
public sealed class Falchion75Lighting
{
    private readonly Compositor _spatial = new(Falchion75Layout.SpatialProfile.LedCount);
    private readonly LedFrame _spatialFrame = new(Falchion75Layout.SpatialProfile.LedCount);
    private readonly LedFrame _output = new(Falchion75Layout.SpatialProfile.LedCount);
    private DecorationMode _bar, _eye;
    public static DecorationMode Normalize(DecorationMode mode) => mode is DecorationMode.Spatial or DecorationMode.Custom ? mode : DecorationMode.FollowKeys;

    public void Configure(IReadOnlyList<IEffectLayer> layers, DecorationMode bar, DecorationMode eye)
    {
        ArgumentNullException.ThrowIfNull(layers);
        _spatial.Clear();
        foreach(var layer in layers) _spatial.Add(layer);
        _bar = Normalize(bar); _eye = Normalize(eye);
    }

    public LedFrame Compose(LedFrame keys, in EffectContext ctx)
    {
        int count = Falchion75Layout.Profile.LedCount;
        if (keys.Length != count) throw new ArgumentException("Unexpected key frame size.", nameof(keys));
        keys.Pixels.CopyTo(_output.Pixels);
        if (_bar != DecorationMode.FollowKeys || _eye != DecorationMode.FollowKeys)
            _spatial.Compose(_spatialFrame, new EffectContext(ctx.Elapsed, ctx.Delta, ctx.NewHits, Falchion75Layout.SpatialProfile));
        for(int i = 0; i < Falchion75Layout.LightBarLedCount; i++)
            _output[count+i] = (i == 12 ? _eye : _bar) != DecorationMode.FollowKeys
                ? _spatialFrame[count+i] : keys[Falchion75Layout.LightBarSourceIndices[i]];
        return _output;
    }
}
