using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
namespace RSS_II_RGB.Core.Effects.Layers;

public enum DecorationEffect { Solid, Music, Rainbow, Breathing, Segmented }
public sealed record DecorationOptions
{
    public DecorationEffect Effect {get;init;}
    public Rgb Color {get;init;}=new(255,0,60);
    public bool MusicRainbow {get;init;}
    public double Speed {get;init;}=1;
    public double Brightness {get;init;}=1;
}

/// <summary>An independently coloured strip or eye; its mask never includes physical keys.</summary>
public sealed class DecorationLayer : IEffectLayer
{
    private readonly SensorState _state;
    private readonly DecorationOptions _options;
    private readonly int _start,_count;
    public DecorationLayer(string id,SensorState state,DecorationOptions options,int start,int count,int zOrder)
    {
        if(start<0 || count<1)throw new ArgumentOutOfRangeException(nameof(start));
        Id=id;_state=state;_options=options;_start=start;_count=count;ZOrder=zOrder;
        Mask=KeyMask.FromIndices(Enumerable.Range(start,count).ToArray());
    }
    public string Id {get;}
    public int ZOrder {get;}
    public KeyMask Mask {get;}
    public BlendMode Blend=>BlendMode.Normal;
    public bool IsComplete=>false;
    public void Render(Span<Rgb> target,in EffectContext ctx)
    {
        var frame=_state.Audio;
        Render(target,ctx,frame.IsFresh(Environment.TickCount64)?frame:AudioFrame.Empty);
    }
    public void Render(Span<Rgb> target,in EffectContext ctx,AudioFrame audio)
    {
        double speed=Safe(_options.Speed,.1,5,1),brightness=Safe(_options.Brightness,0,1,1),time=ctx.Elapsed.TotalSeconds*speed;
        double level=Math.Sqrt(Safe(Math.Max(audio.Volume,audio.Bass),0,1,0));
        for(int n=0;n<_count && _start+n<target.Length;n++)
        {
            double x=_count==1?0:n/(double)(_count-1),light=1;
            if(_options.Effect==DecorationEffect.Music)light=level*Math.Clamp((level-x)*_count+.5,0,1);
            if(_options.Effect==DecorationEffect.Segmented)light=0;
            if(_options.Effect==DecorationEffect.Breathing)light=.08+.92*(.5+.5*Math.Cos(time*Math.PI));
            target[_start+n]=_options.Effect==DecorationEffect.Rainbow || _options.Effect==DecorationEffect.Music && _options.MusicRainbow
                ?Rgb.FromHsv(x*.8-time*.12,1,brightness*light)
                :_options.Color.Scale((float)(brightness*light));
        }
    }
    private static double Safe(double v,double min,double max,double fallback)=>double.IsFinite(v)?Math.Clamp(v,min,max):fallback;
}
