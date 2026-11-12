using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Effects.Layers;

public sealed partial class MusicLayer
{
    private Rgb NeonSpectrumPixel(double x,double y,double time,double band,double edge)
    {
        double light=Fill(SpectrumHeight(y),band,edge)*band;
        return _options.Rainbow
            ? Rgb.FromHsv(x*.28-time*.16,_options.Saturation,light)
            : Rgb.Lerp(_color,Rgb.White,(float)(1-_options.Saturation)).Scale((float)light);
    }

    private Rgb RhythmFirePixel(double x,double y,double time,AudioFrame frame,double energy,double band,double edge)
    {
        double light=Fill(SpectrumHeight(y),band*.7+DisplayLevel(frame.Bass)*.4,edge)*energy;
        return _options.Rainbow
            ? Rgb.FromHsv(.02+Unit(y)*.13,_options.Saturation,light)
            : Rgb.Lerp(_color,Rgb.White,(float)(1-_options.Saturation)).Scale((float)light);
    }

    private Rgb EnergyCorePixel(double circle,double angle,double time,AudioFrame frame,double energy,double width,double rowStep)
    {
        double radius=Math.Min(.42,(.18+energy*.23)*Math.Sqrt(width));
        double rimWidth=Math.Max(rowStep*.6,.02+.025*energy);
        double core=energy*(.85*Fill(circle,radius,rimWidth)+.25*Glow(circle-radius,rimWidth*1.3));
        // Attached angular spikes grow and retract; no detached concentric rings.
        double phase=angle*16+time*.35;
        double tooth=Math.Pow(Math.Max(0,Math.Cos(phase)),.8);
        double local=DisplayLevel(Sample(frame.Spectrum,Frac(angle/Tau)));
        double drive=DisplayLevel(Math.Max(frame.Bass,frame.Onset));
        double reach=(.02+drive*.95+local*.45)*tooth;
        double outside=Unit((circle-radius)/Math.Max(.001,rimWidth));
        double spikes=energy*.45*Fill(circle,radius+reach,rimWidth)*outside;
        double light=Unit(Math.Max(core,spikes));
        double hue=(circle<=radius ? .95-Math.Min(1,circle/Math.Max(.001,radius))*.30 : .65+.12*tooth)+time*.12;
        return _options.Rainbow ? Rgb.FromHsv(hue,_options.Saturation,light)
            : CustomMusicGradient(Unit(circle/Math.Max(.001,radius+reach)),light);
    }

    private Rgb CustomMusicGradient(double position,double light)
    {
        double r=_color.R/255.0,g=_color.G/255.0,b=_color.B/255.0;
        double max=Math.Max(r,Math.Max(g,b)),min=Math.Min(r,Math.Min(g,b)),delta=max-min;
        double hue=delta<=0?0:(max==r?(g-b)/delta:max==g?(b-r)/delta+2:(r-g)/delta+4)/6;
        double saturation=max<=0?0:delta/max;
        return Rgb.FromHsv(hue+position*.16,saturation*_options.Saturation*(1-position*.2),max*light);
    }
}
