using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Rendering;
namespace RSS_II_RGB.Core.Effects.Layers;
public sealed partial class MusicLayer
{
    private double _prismKick,_prismCos,_prismSin,_prismScale,_prismHue;
    private void PreparePrism(double now,AudioFrame frame)
    {
        _prismKick=0;
        foreach(var pulse in _pulses)
        {
            double age=(now-pulse.Time)*_options.Speed;
            _prismKick=Math.Max(_prismKick,pulse.Strength*Math.Exp(-age*5));
        }
        double time=now*_options.Speed,turn=time*.25+_beat*.43;
        _prismCos=Math.Cos(turn);_prismSin=Math.Sin(turn);
        _prismScale=(.7+Unit(frame.Bass*3)*1.8+_prismKick*.6)/_options.Width;
        _prismHue=time*.11+_beat*.11;
    }
    private Rgb PrismPulse(double x,double y,double time,double energy)
    {
        // Broad mirrored colour facets survive coarse LED sampling; bass turns and expands them.
        double dx=(x-.5)*2,dy=(y-.5)*2,u=dx*_prismCos+dy*_prismSin,v=-dx*_prismSin+dy*_prismCos;
        double facet=Math.Abs(Math.Sin((Math.Abs(u)+Math.Abs(v)*.65)*_prismScale*1.5-time*.35));
        double shade=.5+.5*Math.Sin((u-v)*1.6+time*.45);
        double light=energy*(.55+.45*Math.Max(_prismKick,shade));
        // Two broad colour regions with a short transition keep neighbouring LEDs coherent.
        double palette=Unit((facet-.4)/.2);
        double hue=_prismHue+palette*.20;
        return _options.Rainbow?Rgb.FromHsv(hue,_options.Saturation,light):CustomMusicGradient(palette,light);
    }
}
