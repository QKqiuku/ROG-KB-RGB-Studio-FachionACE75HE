using System.Globalization;
using System.Text.Json;
using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.Core.Animation;

public sealed partial class AtkScene
{
    private static Layer Procedural(string type, JsonElement config, KeyboardProfile profile)
    {
        if (type is not ("amplitude_bars" or "stereo_spectrum" or "static" or "spectrum" or "pulse"))
            throw new InvalidDataException($"Unsupported effect: {type}.");
        return new Layer([], [], Number(config,"speed",1,.1,5), Number(config,"brightness",100,0,100)/100,
            Number(config,"opacity",1,0,1), true, new AudioLayer(type, config, profile));
    }

    private static void ParseModules(JsonElement scene, KeyboardProfile profile, List<Layer> layers)
    {
        if (!scene.TryGetProperty("animations",out var modules)) return;
        if (modules.ValueKind != JsonValueKind.Array || modules.GetArrayLength()>8) throw new InvalidDataException("Invalid module list.");
        foreach(var module in modules.EnumerateArray())
        {
            if(module.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Invalid module.");
            if(!Flag(module,"visible",true)) continue;
            if(!module.TryGetProperty("type",out var type) || type.ValueKind!=JsonValueKind.String || type.GetString()!="AudioFrequency")
                throw new InvalidDataException("Unsupported animation module.");
            bool autoLayout=scene.TryGetProperty("settings",out var settings) && settings.ValueKind==JsonValueKind.Object && Flag(settings,"autoLayout",false);
            if(!autoLayout) throw new InvalidDataException("AudioFrequency requires automatic layout; exported scenes omit the device canvas placement needed for absolute positioning.");
            layers.Add(new Layer([],[],1,1,1,true,new AudioLayer("AudioFrequency",module,profile)));
        }
    }

    private sealed class AudioLayer
    {
        private readonly string _type;
        private readonly KeyboardProfile _profile;
        private readonly Rgb[] _colors;
        private readonly double[] _stops;
        private readonly double _sensitivity, _smoothing, _speed, _decay, _minimum;
        private readonly bool _mirror;
        private readonly double[] _levels;
        private readonly int _count, _kernel;
        private readonly double _width,_gap,_rate,_baseHeight,_double,_x,_y,_scale;
        private long _generation = long.MinValue;
        private TimeSpan _last;
        private bool _initialized;
        public bool UsesAudio => _type is "amplitude_bars" or "stereo_spectrum" or "AudioFrequency";
        public AudioLayer(string type, JsonElement config, KeyboardProfile profile)
        {
            _type=type; _profile=profile;
            using var empty = JsonDocument.Parse("{}");
            var parameters=type=="AudioFrequency" && config.TryGetProperty("data",out var data)?data:
                config.TryGetProperty(type=="AudioFrequency"?"params":"customParams",out var p)?p:empty.RootElement;
            if(parameters.ValueKind!=JsonValueKind.Object) throw new InvalidDataException("Invalid parameters.");
            _sensitivity=Number(parameters,"sensitivity",type=="stereo_spectrum"?.8: type=="spectrum"?.7:1,0,10);
            _smoothing=Number(parameters,"smoothing",.25,0,1); _mirror=Flag(parameters,"mirrorMode",true);
            _speed=Number(config,"speed",1,.1,5); _decay=Number(parameters,"decay",.6,.001,10); _minimum=Number(parameters,"minBrightness",.05,0,1);
            _count=Dimension(parameters,"count",64,64); _kernel=Dimension(parameters,"kernelSize",3,25);
            _width=Number(parameters,"width",7,.01,100); _gap=Number(parameters,"gap",0,0,100);
            _rate=Number(parameters,"rate",13,0,25); _baseHeight=Number(parameters,"baseHeight",3,0,1000);
            _double=Number(parameters,"type",1,0,1)==0?2:1; _scale=Number(parameters,"scale",2,.01,100);
            _x=50;_y=150;
            if(parameters.TryGetProperty("pos",out var pos)) { if(pos.ValueKind!=JsonValueKind.Object) throw new InvalidDataException("Invalid module position."); _x=Number(pos,"x",50,-10000,10000);_y=Number(pos,"y",150,-10000,10000); }
            var colors=new List<Rgb>();var stops=new List<double>();
            if(type=="AudioFrequency" && config.TryGetProperty("fill",out var fill))
            {
                if(fill.ValueKind==JsonValueKind.String) { colors.Add(Color(fill)); stops.Add(0); }
                else if(fill.ValueKind==JsonValueKind.Object && fill.TryGetProperty("stops",out var source) && source.ValueKind==JsonValueKind.Array && source.GetArrayLength() is >0 and <=32)
                {
                    if(fill.TryGetProperty("type",out var kind) && kind.GetString()!="linear") throw new InvalidDataException("Unsupported module fill.");
                    int i=0;foreach(var stop in source.EnumerateArray()) { colors.Add(Color(stop.ValueKind==JsonValueKind.String?stop:stop.GetProperty("color")));stops.Add(stop.ValueKind==JsonValueKind.String?(double)i/Math.Max(1,source.GetArrayLength()-1):Number(stop,"offset",0,0,1));i++; }
                    if(!stops.SequenceEqual(stops.Order())) throw new InvalidDataException("Unordered gradient stops.");
                }
                else throw new InvalidDataException("Invalid module fill.");
            }
            else if(config.TryGetProperty("colors",out var source))
            {
                if(source.ValueKind!=JsonValueKind.Array || source.GetArrayLength() is <1 or >32) throw new InvalidDataException("Invalid effect colours.");
                foreach(var color in source.EnumerateArray()) colors.Add(Color(color));
            }
            if(colors.Count==0) colors.AddRange(type=="static"?[new(255,255,255)]:type=="stereo_spectrum"?[new(255,0,68),new(0,255,136),new(68,136,255)]:[new(0,255,102),new(255,204,0),new(255,34,0)]);
            _colors=colors.ToArray(); _stops=stops.Count>0?stops.ToArray():Enumerable.Range(0,colors.Count).Select(i=>(double)i/Math.Max(1,colors.Count-1)).ToArray();
            _levels=new double[Math.Max(profile.Cols,_count)];
        }
        private static Rgb Color(JsonElement value)
        {
            if(value.ValueKind!=JsonValueKind.String) throw new InvalidDataException("Invalid colour.");
            var text=value.GetString()!;
            if(text.Length!=7 || text[0]!='#' || !uint.TryParse(text.AsSpan(1),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out var rgb)) throw new InvalidDataException("Invalid colour.");
            return new((byte)(rgb>>16),(byte)(rgb>>8),(byte)rgb);
        }
        private Rgb Gradient(double t)
        {
            int i=0;while(i<_colors.Length-1 && t>_stops[i+1]) i++;
            if(i==_colors.Length-1)return _colors[i];
            double f=Math.Clamp((t-_stops[i])/Math.Max(.000001,_stops[i+1]-_stops[i]),0,1);
            return new((byte)Math.Round(_colors[i].R+(_colors[i+1].R-_colors[i].R)*f),(byte)Math.Round(_colors[i].G+(_colors[i+1].G-_colors[i].G)*f),(byte)Math.Round(_colors[i].B+(_colors[i+1].B-_colors[i].B)*f));
        }
        private static double Safe(double v)=>double.IsFinite(v)?Math.Clamp(v,0,1):0;
        public void Render(Span<Rgb> target, TimeSpan elapsed, AudioFrame? audio, double brightness, double opacity)
        {
            if(audio is not null && !audio.IsFresh(Environment.TickCount64)) audio=null;
            bool reset=!_initialized || elapsed<_last || audio is not null && audio.Generation!=_generation;
            double deltaSeconds=reset ? 1d/30 : Math.Max(0,(elapsed-_last).TotalSeconds);
            if(reset) {Array.Clear(_levels);_generation=audio?.Generation??long.MinValue;}
            _initialized=true;_last=elapsed;
            int columns=_type=="AudioFrequency"?_count:_profile.Cols;
            if(_type=="stereo_spectrum")
            {
                double[] bands=[audio?.Bass??0,audio?.Mid??0,audio?.Treble??0];
                for(int i=0;i<3;i++)_levels[i]+=(Math.Min(1,Safe(bands[i])*_sensitivity)-_levels[i])*(1-Math.Pow(_smoothing,deltaSeconds*30));
            }
            else if(UsesAudio)
            {
                var spectrum=audio?.Spectrum??[];
                for(int col=0;col<columns;col++)
                {
                    double level=0;int start=col*spectrum.Length/columns,end=Math.Min(spectrum.Length,Math.Max(start+1,(col+1)*spectrum.Length/columns));
                    for(int i=start;i<end;i++) level=Math.Max(level,Safe(spectrum[i]));
                    if(_type=="AudioFrequency")
                    {
                        level=0;double weight=0;
                        int centre=col*spectrum.Length/columns;
                        for(int k=0;k<_kernel;k++)
                        {
                            int offset=k-_kernel/2;double w=Math.Exp(-offset*offset/8d);weight+=w;
                            int bin=centre+offset;if(bin>=0 && bin<spectrum.Length)level+=Safe(spectrum[bin])*w;
                        }
                        level/=Math.Max(weight,.000001);
                    }
                    level=Math.Min(1,level*_sensitivity);
                    if(_type=="AudioFrequency") _levels[col]+=(level-_levels[col])*(1-Math.Exp(-deltaSeconds/(_levels[col]<=level?.012:.028)));
                    else _levels[col]=reset || level>=_levels[col]?level:_levels[col]+(level-_levels[col])*(1-Math.Pow(.75,deltaSeconds*30));
                }
            }
            foreach(var key in _profile.Keys) Sample(target,key.Index,key.Row,key.Col,elapsed.TotalMilliseconds*_speed,brightness,opacity);
            for(int i=_profile.LedCount;i<target.Length;i++) Sample(target,i,0,Falchion75Layout.LightBarSourceIndices[i-_profile.LedCount],elapsed.TotalMilliseconds*_speed,brightness,opacity);
        }
        private void Sample(Span<Rgb> target,int index,int row,int col,double time,double brightness,double opacity)
        {
            double x=(double)col/Math.Max(1,_profile.Cols-1),y=(double)row/Math.Max(1,_profile.Rows-1), intensity=1;
            Rgb color=_colors[0];
            switch(_type)
            {
                case "amplitude_bars":
                    int height=_profile.Rows-1-row;
                    if(_levels[Math.Min(col,_profile.Cols-1)]*_profile.Rows<=height) return;
                    color=Gradient((double)height/Math.Max(1,_profile.Rows-1));break;
                case "stereo_spectrum":
                    int band=y<.33?2:y<.66?1:0;
                    double level=_levels[band],position=_mirror?Math.Abs(x-.5)*2:x<.5?x*2:(x-.5)*2;
                    if(position>=level)return;
                    intensity=(1-(level-position)*.3)*level*(1+Math.Sin(time/200)*.1*level);
                    color=_colors[Math.Min(band,_colors.Length-1)];break;
                case "spectrum":
                    intensity=Math.Min(1,(Math.Sin(time/3000*Math.PI*2+x*Math.PI)+1)/2*_sensitivity*(1-y*.4));
                    color=_colors.Length>=3?Gradient(x):_colors[0];break;
                case "pulse":
                    double phase=time%2000/2000;
                    intensity=Math.Max(_minimum,phase<.1?phase/.1:Math.Pow(1-(phase-.1)/.9,2/_decay));
                    color=Gradient(intensity);break;
                case "AudioFrequency":
                    // Automatic layout centres the bar group and aligns its bottom with the keyboard bounds.
                    double groupWidth=(_count-1)*(_width/2+_gap)+_width/2;
                    double localX=(col+.5)*groupWidth/_profile.Cols;
                    double localY=(row+.5-_profile.Rows)*groupWidth/_profile.Cols-_baseHeight*_double;
                    int bar=(int)Math.Floor(localX/(_width/2+_gap));
                    if(bar<0 || bar>=_count || localX-bar*(_width/2+_gap)>_width/2)return;
                    double amount=_levels[bar]*10*_rate,heightPixels=(amount+_baseHeight)*_double;
                    if(localY < -amount || localY > heightPixels-amount)return;
                    color=Gradient((localY+amount)/Math.Max(.01,heightPixels));break;
            }
            var previous=target[index];
            target[index]=new Rgb(Blend(color.R,previous.R),Blend(color.G,previous.G),Blend(color.B,previous.B));
            byte Blend(byte channel,byte background)=>(byte)Math.Clamp(Math.Round(255-(255-Math.Round(channel*Math.Clamp(intensity,0,1)*brightness))*(255-background)/255),0,255);
        }
    }
}
