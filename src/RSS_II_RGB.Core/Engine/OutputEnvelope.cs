using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;

namespace RSS_II_RGB.Core.Engine;

public enum OutputEnvelopeMode { Constant, Breathing, BeatFlash }
public sealed record OutputEnvelopeOptions
{
    public OutputEnvelopeMode Mode {get;init;}
    public double Opacity {get;init;}=1;
    public double Minimum {get;init;}=.1;
    public double Speed {get;init;}=1;
}

/// <summary>Final attenuation after every key, overlay and decorative region has been composed.</summary>
public sealed class OutputEnvelope(SensorState state,OutputEnvelopeOptions options)
{
    private long _generation=-1,_beat;
    private double _lastBeat=double.NegativeInfinity,_previous=-1;
    public void Apply(Span<Rgb> pixels,TimeSpan elapsed)
    {
        var frame=state.Audio;
        Apply(pixels,elapsed,frame.IsFresh(Environment.TickCount64)?frame:AudioFrame.Empty with {Generation=frame.Generation});
    }
    public void Apply(Span<Rgb> pixels,TimeSpan elapsed,AudioFrame audio)
    {
        double now=elapsed.TotalSeconds;
        if(_generation!=audio.Generation || now<_previous) {_lastBeat=double.NegativeInfinity;_beat=0;}
        _generation=audio.Generation;_previous=now;
        if(audio.BassBeatId>_beat) {_lastBeat=now;_beat=audio.BassBeatId;}
        double max=Safe(options.Opacity,0,1,1),min=Math.Min(max,Safe(options.Minimum,0,1,.1)),speed=Safe(options.Speed,.1,5,1);
        double pulse=options.Mode switch
        {
            OutputEnvelopeMode.Breathing=>.5+.5*Math.Cos(now*speed*Math.PI),
            OutputEnvelopeMode.BeatFlash=>Math.Exp(-Math.Max(0,now-_lastBeat)*6*speed),
            _=>1
        };
        float alpha=(float)(min+(max-min)*pulse);
        for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].Scale(alpha);
    }
    private static double Safe(double v,double min,double max,double fallback)=>double.IsFinite(v)?Math.Clamp(v,min,max):fallback;
}
