using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Effects;

/// <summary>A small row-major colour field; published buffers remain immutable.</summary>
public interface IScreenColorSource
{
    int Width { get; }
    int Height { get; }
    ReadOnlyMemory<Rgb> Colors { get; }
}
