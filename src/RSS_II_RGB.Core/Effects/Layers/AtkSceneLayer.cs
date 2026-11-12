using RSS_II_RGB.Core.Animation;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Effects.Layers;

public sealed class AtkSceneLayer(string id, AtkScene scene, KeyMask mask, int zOrder = 0, double speed = 1, SensorState? sensors = null) : IEffectLayer
{
    public string Id => id;
    public int ZOrder => zOrder;
    public BlendMode Blend => BlendMode.Normal;
    public KeyMask Mask => mask;
    public bool IsComplete => false;
    public void Render(Span<Rgb> target, in EffectContext ctx)
    {
        if (scene.PlaybackStart is null || ctx.Elapsed < scene.PlaybackStart.Value) scene.PlaybackStart = ctx.Elapsed;
        scene.Render(target, TimeSpan.FromMilliseconds((ctx.Elapsed - scene.PlaybackStart.Value).TotalMilliseconds * Math.Clamp(speed, 0.1, 5)), sensors?.Audio);
    }
}
