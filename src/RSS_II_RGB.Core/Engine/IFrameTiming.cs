namespace RSS_II_RGB.Core.Engine;

/// <summary>Acquire timer precision only while rendering; disposal restores the previous request.</summary>
public interface IFrameTiming
{
    IDisposable Enter();
}
