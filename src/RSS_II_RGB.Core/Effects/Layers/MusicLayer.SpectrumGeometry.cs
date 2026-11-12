namespace RSS_II_RGB.Core.Effects.Layers;

public sealed partial class MusicLayer
{
    // The decorative strip is one LED row above the function row.
    private static double SpectrumHeight(double y)=>(1-y)/1.2;
    public static (double Top,double Bottom) CanvasBounds(MusicKind kind)=>kind switch
    {
        MusicKind.SoundWave or MusicKind.BidirectionalSpectrum =>(-.2,1.2),
        MusicKind.SpectrumBars or MusicKind.ClassicEqualizer or MusicKind.MirrorSpectrum
            or MusicKind.NeonSpectrum or MusicKind.RhythmFire =>(-.2,1),
        _ =>(0,1)
    };
}
