using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;

namespace RSS_II_RGB.Core.Effects.Layers;

/// <summary>Independent live-audio reconstructions inferred from public visual references.</summary>
public sealed partial class MusicLayer : IEffectLayer
{
    private const double Tau = Math.PI * 2;
    private readonly MusicKind _kind;
    private readonly SensorState _state;
    private readonly Rgb _color;
    private readonly PatternOptions _options;
    private readonly List<(double Time, long Beat, double Strength)> _pulses = [];
    private readonly List<(double Time, double[] Bands)> _history = [];
    private long _generation = -1, _beat, _sequence = -1, _timestamp;
    private double _lastTime = -1, _wavePeak = .04;

    public MusicLayer(string id, MusicKind kind, SensorState state, Rgb color, KeyMask mask,
        PatternOptions? options = null, int zOrder = 0)
    {
        Id = id; _kind = kind; _state = state; _color = color; Mask = mask; ZOrder = zOrder;
        options ??= new();
        _options = options with
        {
            Speed = Safe(options.Speed, .1, 5, 1), Width = Safe(options.Width, .25, 3, 1),
            Density = Safe(options.Density, .05, 1, .3), Saturation = Safe(options.Saturation, 0, 1, 1),
            Direction = Safe(options.Direction, 0, 360, 0)
        };
    }
    public string Id { get; }
    public int ZOrder { get; }
    public KeyMask Mask { get; }
    public BlendMode Blend => BlendMode.Normal;
    public bool IsComplete => false;
    public int ActivePulseCount => _pulses.Count;
    public int HistoryCount => _history.Count;

    public void Render(Span<Rgb> target, in EffectContext ctx)
    {
        var frame = _state.Audio;
        Render(target, ctx, frame.IsFresh(Environment.TickCount64) ? frame : AudioFrame.Empty with { Generation = frame.Generation });
    }

    // Explicit snapshots let offscreen previews and tests use the same renderer without a wall clock.
    public void Render(Span<Rgb> target, in EffectContext ctx, AudioFrame frame)
    {
        double now = ctx.Elapsed.TotalSeconds;
        double energy=Prepare(frame,now);
        double angle = _options.Direction * Tau / 360, cos = Math.Cos(angle), sin = Math.Sin(angle);
        for (int i = 0; i < target.Length; i++)
        {
            var key = ctx.Layout.ByIndex(i);
            double x = (double)key.Col / Math.Max(1, ctx.Layout.Cols - 1);
            double y = (double)key.Row / Math.Max(1, ctx.Layout.Rows - 1);
            if (_options.MirrorX) x = 1-x;
            if (_options.MirrorY) y = 1-y;
            double u = (x-.5)*cos+(y-.5)*sin+.5, v = -(x-.5)*sin+(y-.5)*cos+.5;
            if(_kind==MusicKind.EnergyCore) {u=x;v=y;}
            target[i] = Pixel(u, v, i, now, frame, energy, 1.0 / Math.Max(1, ctx.Layout.Rows - 1),(ctx.Layout.Cols-1.0)/Math.Max(1,ctx.Layout.Rows-1));
        }
    }

    /// <summary>Evaluate the same field at actual canvas pixels, independently of keyboard LED count.</summary>
    public void RenderCanvas(Span<Rgb> target,int width,int height,TimeSpan elapsed,AudioFrame frame,CancellationToken ct=default,double top=0,double bottom=1)
    {
        if(width<2 || height<2 || width>2048 || height>1024 || target.Length!=width*height || !double.IsFinite(top) || !double.IsFinite(bottom) || top>=bottom || top< -1 || bottom>2)
            throw new ArgumentOutOfRangeException(nameof(width));
        double now=elapsed.TotalSeconds,energy=Prepare(frame,now);
        double angle=_options.Direction*Tau/360,cos=Math.Cos(angle),sin=Math.Sin(angle);
        for(int row=0;row<height;row++)
        {
            ct.ThrowIfCancellationRequested();
            for(int col=0;col<width;col++)
            {
                double x=col/(double)(width-1),y=top+(bottom-top)*row/(height-1);
                if(_options.MirrorX)x=1-x;if(_options.MirrorY)y=1-y;
                double u=(x-.5)*cos+(y-.5)*sin+.5,v=-(x-.5)*sin+(y-.5)*cos+.5;
                if(_kind==MusicKind.EnergyCore) {u=x;v=y;}
                int index=row*width+col;
                target[index]=Pixel(u,v,index,now,frame,energy,(bottom-top)/(height-1),(width-1.0)/(height-1)/(bottom-top));
            }
        }
    }

    private double Prepare(AudioFrame frame,double now)
    {
        bool reset=_generation != frame.Generation || now < _lastTime || (frame.TimestampMs > 0 && frame.TimestampMs < _timestamp);
        if (reset)
        {
            _pulses.Clear(); _history.Clear(); _sequence = -1; _beat = 0;
        }
        _generation = frame.Generation; _lastTime = now;
        if (frame.TimestampMs > 0) _timestamp = frame.TimestampMs;
        // PCM energy is linear; map it to a perceptual display range before using it as geometry.
        double energy = DisplayLevel(frame.Volume);
        _wavePeak = .04;
        foreach(double sample in frame.Waveform) if(double.IsFinite(sample)) _wavePeak=Math.Max(_wavePeak,Math.Abs(sample));
        long eventId=_kind is MusicKind.Shockwave or MusicKind.RhythmOrbit?frame.BassBeatId:_kind==MusicKind.BeatFlash?frame.LowBeatId:frame.BeatId;
        double strength=_kind is MusicKind.Shockwave or MusicKind.RhythmOrbit?DisplayLevel(Math.Max(frame.Bass,frame.BassOnset)):
            _kind==MusicKind.BeatFlash?DisplayLevel(Math.Max(frame.Bass+frame.Mid*.2,frame.LowOnset)):Math.Max(energy,DisplayLevel(frame.Onset));
        if (eventId > _beat && energy > .001 && strength>.001)
        {
            _pulses.Add((now, eventId, strength));
            if (_pulses.Count > 32) _pulses.RemoveAt(0);
        }
        _beat = Math.Max(_beat, eventId);
        if (frame.Sequence != _sequence)
        {
            _sequence = frame.Sequence;
            if (energy > .001)
            {
                var bands = new double[Math.Min(64, frame.Spectrum.Length)];
                for (int n = 0; n < bands.Length; n++) bands[n] = Unit(frame.Spectrum[n]);
                _history.Add((now, bands));
                if (_history.Count > 64) _history.RemoveAt(0);
            }
        }
        _pulses.RemoveAll(p => (now - p.Time) * _options.Speed > 2);
        _history.RemoveAll(h => (now - h.Time) * _options.Speed > 2);
        if(_kind==MusicKind.ClassicEqualizer) PrepareEqualizer(frame,now,reset);
        if(_kind==MusicKind.RhythmOrbit)PreparePrism(now,frame);
        return energy;
    }

    private Rgb Pixel(double x, double y, int index, double now, AudioFrame f, double e, double rowStep,double aspect)
    {
        if(_kind==MusicKind.ClassicEqualizer && TryEqualizerCap(x,y,rowStep,aspect,e,out var cap))return cap;
        if (e <= .001 && _kind is not (MusicKind.ClassicEqualizer or MusicKind.SpectrumWaterfall or MusicKind.BeatFlash or
            MusicKind.Shockwave or MusicKind.BeatSparks or MusicKind.BeatRipples or MusicKind.BeatSweep or
            MusicKind.RhythmStars or MusicKind.BeatChecker or MusicKind.Party or MusicKind.MusicPuddles))
            return Rgb.Black;
        double t = now*_options.Speed, w = _options.Width, dx = x-.5, dy = y-.5;
        double r = Math.Sqrt(dx*dx+dy*dy);
        double a = _kind is MusicKind.SpectrumRing or MusicKind.BottomSpectrumRing
            or MusicKind.BeatRipples or MusicKind.RadialSpectrum ? Math.Atan2(dy,dx)/Tau : 0;
        double band = DisplayLevel(Sample(f.Spectrum,x)), hue = x*.75, light = 0;
        double edge = .045*w;
        switch (_kind)
        {
            case MusicKind.SpectrumBars: light = Fill(SpectrumHeight(y),band,edge)*band; break;
            case MusicKind.ClassicEqualizer:
                int spectrumBins=Math.Min(32,f.Spectrum.Length);
                band=spectrumBins==0 || e<=.001?0:DisplayLevel(f.Spectrum[Math.Min(spectrumBins-1,(int)(Unit(x)*spectrumBins))]);
                light = Fill(SpectrumHeight(y),band,edge)*band; hue = Unit(y)*.34;
                break;
            case MusicKind.MirrorSpectrum:
                band=DisplayLevel(Sample(f.Spectrum,Math.Abs(x-.5)*2)); light=Fill(SpectrumHeight(y),band,edge)*band; hue=Math.Abs(x-.5); break;
            case MusicKind.NeonSpectrum:
                return NeonSpectrumPixel(x,y,t,band,edge);
            case MusicKind.FlowNeonSpectrum:
                light=band*(.25*Fill(SpectrumHeight(y),band,edge)+.75*Glow(1-y-band,edge*2));
                hue=x*.65+.48+(_kind==MusicKind.FlowNeonSpectrum?t*.12:0); break;
            case MusicKind.SoftSpectrum:
                light=band*Math.Exp(-Math.Max(0,1-y-band)*5/w); hue=x*.8+.45; break;
            case MusicKind.SoundWave:
                return SoundWaves(x,y,t,f.Bass,rowStep);
            case MusicKind.SpectrumWaterfall:
                foreach(var h in _history)
                {
                    double age=(now-h.Time)*_options.Speed;
                    light=Math.Max(light,DisplayLevel(Sample(h.Bands,x))*Glow(y-age/2, .13*w)*(1-age/2));
                }
                hue=x*.5+y*.4; break;
            case MusicKind.RhythmOrbit:
                return PrismPulse(x,y,t,e);
            case MusicKind.EnergyCore:
                double circle=Math.Sqrt(dx*dx*aspect*aspect+dy*dy);
                return EnergyCorePixel(circle,Math.Atan2(dy,dx*aspect),t,f,e,w,rowStep);
            case MusicKind.BottomEnergyCore:
                if(_kind==MusicKind.BottomEnergyCore) r=Math.Sqrt(dx*dx+(y-1)*(y-1));
                light=e*Math.Exp(-r*r/(.08+e*.32)/w); hue=.95-r*.65; break;
            case MusicKind.VolumeBreathing: light=e; hue=.35+t*.03; break;
            case MusicKind.RhythmHeart:
                light=e*Heart(dx/(.65+e*.4),dy/(.65+e*.4)); hue=.97; break;
            case MusicKind.SpectrumColors: light=band*(.6+.4*y); hue=x*.8; break;
            case MusicKind.BidirectionalSpectrum: light=band*Fill(Math.Abs(y-.5)/.7,band,edge); hue=.45+x*.3; break;
            case MusicKind.SpectrumRing:
            case MusicKind.BottomSpectrumRing:
                if(_kind==MusicKind.BottomSpectrumRing){dy=y-1;r=Math.Sqrt(dx*dx+dy*dy);a=Math.Atan2(dy,dx)/Tau;}
                band=DisplayLevel(Sample(f.Spectrum,Frac(a))); light=band*Glow(r-(.16+band*.7),.12*w); hue=a+.6; break;
            case MusicKind.BassSurge: light=DisplayLevel(f.Bass)*Glow(1-y-DisplayLevel(f.Bass)*.65,.18*w); hue=.85; break;
            case MusicKind.RhythmFire:
                return RhythmFirePixel(x,y,t,f,e,band,edge);
            case MusicKind.WaveformBounce:
                light=e*Fill(1-y,Math.Abs(Wave(f.Waveform,x))*.85,edge); hue=.43+x*.15; break;
            case MusicKind.CentralRhythm: light=e*Glow(dx,.10*w+e*.45)*Glow(dy,.10*w+e*.40); hue=.43; break;
            case MusicKind.RowBars:
                band=DisplayLevel(Sample(f.Spectrum,y)); light=band*Fill(Math.Min(x,1-x)*2,band,edge); hue=.46+y*.13; break;
            case MusicKind.SoundDiffusion: light=e*Fill(r,e*.8,edge)*(.35+.65*Glow(r-e*.5,.2*w)); hue=.46+r*.15; break;
            case MusicKind.HeartPierce:
                light=e*Math.Max(Heart(dx,dy),Glow(y-.12-Wave(f.Waveform,x)*.1,edge)); hue=Heart(dx,dy)>.5?.96:.45; break;
            case MusicKind.SLine:
                light=e*Glow(y-(.5+Wave(f.Waveform,x)*.4),edge); hue=.43+x*.2; break;
            case MusicKind.PlasmaLightning:
                light=e*Glow(dx-Wave(f.Waveform,y)*.5,.035*w)*( .4+.6*DisplayLevel(f.Treble)); hue=.3+t*.07; break;
            case MusicKind.RainbowCorridor:
                light=e*(.2+.8*Glow(Math.Sin((Math.Max(Math.Abs(dx),Math.Abs(dy))*5-e-t*.15)*Math.PI),.5*w)); hue=r*2+t*.08; break;
            case MusicKind.FlameEye:
                light=e*Glow(Math.Sqrt(dx*dx+dy*dy*3)-(.18+DisplayLevel(f.Bass)*.22),.13*w);
                hue=.02+DisplayLevel(f.Treble)*.12; break;
            case MusicKind.RadialSpectrum:
                band=DisplayLevel(Sample(f.Spectrum,Frac(a)));light=band*Fill(r,band*.7,edge)*(.25+.75*Glow(Math.Sin(a*Tau*8),.7));hue=a+.65;break;
            default:
                foreach(var p in _pulses)
                {
                    double age=(now-p.Time)*_options.Speed, fade=Math.Max(0,1-age/2), q=0;
                    double px=Hash(p.Beat*17), py=Hash(p.Beat*29);
                    switch(_kind)
                    {
                        case MusicKind.BeatFlash:q=Math.Exp(-age*5);hue=Hash(p.Beat);break;
                        case MusicKind.Shockwave:q=Glow(Math.Sqrt(dx*dx*aspect*aspect+dy*dy)-age*2.6,.16*w);hue=.85+age*.2;break;
                        case MusicKind.BeatRipples:q=Glow(Math.Sin((r-age*.4)*Tau*3),.25*w)*Math.Exp(-r);hue=age*.3+a;break;
                        case MusicKind.BeatSweep:q=Glow(x-age*.7,.1*w);hue=Hash(p.Beat);break;
                        case MusicKind.BeatChecker:q=((int)Math.Floor(x*10)+(int)Math.Floor(y*6)+p.Beat)%2==0?1:0;hue=Hash(p.Beat);break;
                        case MusicKind.BeatSparks:q=Hash(index*31+p.Beat*7)<_options.Density?Math.Exp(-age*3):0;hue=Hash(index+p.Beat);break;
                        case MusicKind.RhythmStars:q=Hash(index*17+p.Beat*11)<_options.Density*.6?Math.Exp(-age):0;hue=.6;break;
                        case MusicKind.Party:q=Glow(r-age*.7,.12*w)+((Hash(index+p.Beat*13)<_options.Density)?Math.Exp(-age*3):0);hue=Hash(p.Beat)+x+y;break;
                        case MusicKind.MusicPuddles:q=Glow(Math.Sqrt((x-px)*(x-px)+(y-py)*(y-py))-age*.3,.08*w);hue=px+age*.1;break;
                    }
                    light=Math.Max(light,q*fade*p.Strength);
                }
                break;
        }
        light=Unit(light);
        double saturation = _options.Saturation * (_kind is MusicKind.RhythmStars ? .12 :
            _kind is MusicKind.PlasmaLightning ? .3 : 1);
        return _options.Rainbow ? Rgb.FromHsv(hue,saturation,light) :
            Rgb.Lerp(_color, Rgb.White, (float)(1-_options.Saturation)).Scale((float)light);
    }
    private static double Heart(double x,double y)
    {
        double u=x*2.5,v=-y*2.5+.2, q=u*u+v*v-1;
        return q*q*q-u*u*v*v*v<=0?1:0;
    }
    private static double DisplayLevel(double v) => Math.Pow(Unit(v), .35);
    private double Wave(double[] data,double position) => Math.Clamp(SampleSigned(data,position)/_wavePeak,-1,1);
    private static double Unit(double v)=>Safe(v,0,1,0);
    private static double Safe(double v,double min,double max,double fallback)=>double.IsFinite(v)?Math.Clamp(v,min,max):fallback;
    private static double Fill(double position,double height,double edge)=>Unit((height-position)/edge+.5);
    private static double Glow(double d,double width)=>Math.Exp(-d*d/(width*width));
    private static double Frac(double v)=>v-Math.Floor(v);
    private static double Hash(double v)=>Frac(Math.Sin(v*12.9898+78.233)*43758.5453);
    private static double Sample(double[] data,double p)=>data.Length==0?0:Unit(data[(int)(Unit(p)*(Math.Min(data.Length,256)-1))]);
    private static double SampleSigned(double[] data,double p)=>data.Length==0?0:Safe(data[(int)(Unit(p)*(Math.Min(data.Length,256)-1))],-1,1,0);
}
