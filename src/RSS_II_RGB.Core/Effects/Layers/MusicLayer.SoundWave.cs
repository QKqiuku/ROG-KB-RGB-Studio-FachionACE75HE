using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Effects.Layers;

public sealed partial class MusicLayer
{
    private Rgb SoundWaves(double x,double y,double time,double bass,double rowStep)
    {
        // Bass averages eleven bands; expand that range before mapping displacement.
        double energy=DisplayLevel(bass);
        double r=0,g=0,b=0,amplitude=.10+.60*Unit(bass*3.5),width=.05*_options.Width;
        for(int wave=0;wave<2;wave++)
        {
            double baseline=.5;
            double speed=wave==0?.04:.06;
            double center=baseline+amplitude*Math.Sin(Tau*(x-time*speed)+wave*Tau/3);
            double distance=y-center,half=rowStep*.5;
            // Integrate the beam over the sample footprint so coarse key rows cannot miss it.
            double coverage=width*(TriangleIntegral((distance+half)/width)-TriangleIntegral((distance-half)/width))/rowStep;
            double light=Math.Sqrt(Unit(coverage))*energy;
            var color=_options.Rainbow?Rgb.FromHsv(x*.85-time*.12+wave*.25,_options.Saturation,1):
                Rgb.Lerp((wave==0?_options.WaveColor1:_options.WaveColor2)??_color,Rgb.White,(float)(1-_options.Saturation));
            r+=color.R*light;g+=color.G*light;b+=color.B*light;
        }
        return new Rgb((byte)Math.Clamp(r,0,255),(byte)Math.Clamp(g,0,255),(byte)Math.Clamp(b,0,255));
    }
    private static double TriangleIntegral(double x)=>x<=-1?0:x<0?.5*(x+1)*(x+1):x<1?1-.5*(1-x)*(1-x):1;
}
