using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Effects.Layers;

public enum KeyEffectKind { Spotlight, DarkRipple, DoubleRing, Diamond, Dye, Heart, Lightning, Fireworks, Ripple }

/// <summary>Real-key interaction layers; dark waves multiply, dye replaces, and lit accents overlay the base.</summary>
public sealed class ReactivePatternLayer : IEffectLayer
{
    private readonly Rgb _color;
    private readonly double _brightness;
    private readonly PatternOptions _options;
    private readonly List<KeyHit> _hits=[];
    private Rgb[] _dyeBase=[], _dyeFrame=[];
    private KeyHit? _dyeHit;
    private Rgb _dyeTarget;
    private int _dyeSequence;
    private double _previousTime;

    public ReactivePatternLayer(string id, KeyEffectKind kind, Rgb color, KeyMask mask, PatternOptions options, int zOrder=0, double brightness=1)
    {
        Id=id; Kind=kind; Mask=mask; ZOrder=zOrder; _color=color; _brightness=Safe(brightness,0,1,1);
        _options=options with {Rainbow=options.Rainbow || options.ReactiveRainbow,Speed=Safe(options.Speed,.1,5,1),Width=Safe(options.Width,.25,3,1),Saturation=Safe(options.Saturation,0,1,1)};
    }
    public string Id {get;}
    public KeyEffectKind Kind {get;}
    public int ZOrder {get;}
    public KeyMask Mask {get;}
    public BlendMode Blend => Kind==KeyEffectKind.DarkRipple?BlendMode.Multiply:Kind==KeyEffectKind.Dye?BlendMode.Normal:BlendMode.Additive;
    public bool IsComplete=>false;

    public void Render(Span<Rgb> target,in EffectContext ctx)
    {
        double now=ctx.Elapsed.TotalSeconds;
        if(now<_previousTime) {_hits.Clear();_dyeHit=null;_dyeFrame=[];_dyeBase=[];_dyeSequence=0;}
        _previousTime=now;
        if(Kind==KeyEffectKind.Dye) {RenderDye(target,ctx);return;}
        foreach(var hit in ctx.NewHits)
            if((uint)hit.KeyIndex<(uint)target.Length && Mask.Contains(hit.KeyIndex)) _hits.Add(hit);
        _hits.RemoveAll(h=>(now-h.When.TotalSeconds)*_options.Speed>=2.5);
        if(_hits.Count>32) _hits.RemoveRange(0,_hits.Count-32);
        for(int i=0;i<target.Length;i++)
        {
            Rgb result=Kind==KeyEffectKind.DarkRipple?Rgb.White:Rgb.Black;
            var key=ctx.Layout.ByIndex(i);
            foreach(var hit in _hits)
            {
                var origin=ctx.Layout.ByIndex(hit.KeyIndex);
                double age=Math.Max(0,now-hit.When.TotalSeconds)*_options.Speed;
                double dc=key.Col-origin.Col, dr=key.Row-origin.Row;
                double distance=Math.Sqrt(dc*dc+dr*dr), width=_options.Width;
                double light=0,hue=.57,saturation=.9;
                switch(Kind)
                {
                    case KeyEffectKind.Ripple:
                        light=Math.Max(0,1-Math.Abs(distance-age*9)/width)*Math.Max(0,1-age/2);
                        hue=hit.KeyIndex*.071+age*.2;break;
                    case KeyEffectKind.Spotlight:
                        light=Math.Exp(-distance*distance/(4.5*width*width))*Math.Exp(-age*3);
                        hue=.12;saturation=.2;break;
                    case KeyEffectKind.DarkRipple:
                        double darkness=Math.Exp(-Math.Pow((distance-age*8)/(width*.65),2))*Math.Max(0,1-age/2.5);
                        result=result.Scale((float)(1-.97*darkness*_brightness));continue;
                    case KeyEffectKind.DoubleRing:
                        double first=Math.Exp(-Math.Pow((distance-age*8)/(width*.6),2));
                        double second=age>.16?Math.Exp(-Math.Pow((distance-(age-.16)*8)/(width*.55),2)):0;
                        light=Math.Max(first,second)*Math.Max(0,1-age/1.8);hue=.55+age*.12;break;
                    case KeyEffectKind.Diamond:
                        light=Math.Exp(-Math.Pow((Math.Abs(dc)+Math.Abs(dr)-age*9)/(width*.65),2))*Math.Max(0,1-age/1.8);
                        hue=.55+age*.12;break;
                    case KeyEffectKind.Heart:
                        double scale=(.6+age*3)*width, hx=dc/scale, hy=-dr/scale;
                        double shape=Math.Pow(hx*hx+hy*hy-1,3)-hx*hx*hy*hy*hy;
                        light=shape<=0?Math.Max(0,1-age/1.3):0;hue=.94;break;
                    case KeyEffectKind.Lightning:
                        double zig=Math.Round((Hash(origin.Col*17+key.Row*29)-.5)*1.6);
                        light=Math.Exp(-Math.Pow((dc-zig)*2/width,2))*Math.Max(0,1-Math.Abs(dr)/4.5)*Math.Exp(-Math.Max(0,age-.15)*6);
                        hue=.61;saturation=.22;break;
                    case KeyEffectKind.Fireworks:
                        double x=(double)key.Col/Math.Max(1,ctx.Layout.Cols-1),y=(double)key.Row/Math.Max(1,ctx.Layout.Rows-1);
                        double ox=(double)origin.Col/Math.Max(1,ctx.Layout.Cols-1),oy=(double)origin.Row/Math.Max(1,ctx.Layout.Rows-1);
                        light=FireworkBurst.Light(x,y,ox,oy,age,width,_options.Density,hit.KeyIndex);
                        hue=Hash(hit.KeyIndex*11+5)+Math.Atan2(y-oy,x-ox)/(2*Math.PI)*.12;
                        break;
                }
                if (_options.ReactiveRainbow) hue=(double)key.Col/Math.Max(1,ctx.Layout.Cols-1)+now*.18+age*.12;
                result=Compositor.Blend(result,Colour(hue,light,saturation),BlendMode.Max);
            }
            target[i]=result;
        }
    }

    private void RenderDye(Span<Rgb> target,in EffectContext ctx)
    {
        if(_dyeFrame.Length!=target.Length)
        {
            _dyeFrame=new Rgb[target.Length];_dyeBase=new Rgb[target.Length];
            Array.Fill(_dyeFrame,Colour(.6,.75));Array.Copy(_dyeFrame,_dyeBase,target.Length);
        }
        KeyHit? newest=null;
        foreach(var hit in ctx.NewHits)
            if((uint)hit.KeyIndex<(uint)target.Length && Mask.Contains(hit.KeyIndex)) {newest=hit;_dyeSequence++;}
        if(newest is {} next)
        {
            Array.Copy(_dyeFrame,_dyeBase,target.Length);_dyeHit=next;
            _dyeTarget=Colour(.6-_dyeSequence*.09,.85);
        }
        if(_dyeHit is {} hitOrigin)
        {
            var origin=ctx.Layout.ByIndex(hitOrigin.KeyIndex);
            double age=Math.Max(0,(ctx.Elapsed-hitOrigin.When).TotalSeconds)*_options.Speed;
            for(int i=0;i<target.Length;i++)
            {
                var key=ctx.Layout.ByIndex(i);
                double distance=Math.Sqrt(Math.Pow(key.Col-origin.Col,2)+Math.Pow(key.Row-origin.Row,2));
                double mix=Math.Clamp((age*10-distance)/_options.Width+.5,0,1);
                var dyeTarget=_options.ReactiveRainbow
                    ? Rgb.FromHsv((double)key.Col/Math.Max(1,ctx.Layout.Cols-1)-_dyeSequence*.09,_options.Saturation,.85*_brightness)
                    : _dyeTarget;
                Rgb dyed=Rgb.Lerp(_dyeBase[i],dyeTarget,(float)mix);
                double edge=Math.Exp(-Math.Pow((distance-age*10)/(_options.Width*.45),2));
                _dyeFrame[i]=Rgb.Lerp(dyed,(_options.Rainbow?Rgb.White:_color).Scale((float)_brightness),(float)(.75*edge));
            }
        }
        _dyeFrame.CopyTo(target);
    }
    private Rgb Colour(double hue,double intensity,double saturation=1)=>_options.Rainbow
        ?Rgb.FromHsv(hue,_options.Saturation*saturation,Math.Clamp(intensity*_brightness,0,1)):_color.Scale((float)Math.Clamp(intensity*_brightness,0,1));
    private static double Safe(double v,double min,double max,double fallback)=>double.IsFinite(v)?Math.Clamp(v,min,max):fallback;
    private static double Hash(double x) {double h=Math.Sin(x*12.9898+78.233)*43758.5453;return h-Math.Floor(h);}
}
