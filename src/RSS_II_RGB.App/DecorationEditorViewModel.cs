using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Rendering;
namespace RSS_II_RGB.App;
internal sealed record DecorationChoice(int Value,string Label);
internal sealed partial class DecorationEditorViewModel(Action changed,bool isEye=false):ObservableObject
{
    private bool _loading;
    public DecorationChoice[] Choices {get;}=isEye
        ?[new(0,L.OverallConstant),new(2,L.DecorationRainbowLabel),new(3,L.OverallBreathing)]
        :[new(0,L.OverallConstant),new(1,L.DecorationMusicLabel),new(2,L.DecorationRainbowLabel),new(3,L.OverallBreathing),new(4,L.DecorationSegmentedLabel)];
    public DecorationChoice? SelectedChoice {get=>Choices.FirstOrDefault(c=>c.Value==EffectIndex);set {if(value is not null)EffectIndex=value.Value;}}
    [ObservableProperty] private bool _musicRainbow;
    [ObservableProperty] private int _effectIndex;
    [ObservableProperty] private Color _color=Avalonia.Media.Color.FromRgb(255,0,60);
    [ObservableProperty] private double _speed=1;
    [ObservableProperty] private double _brightness=100;
    public bool HasSpeed=>EffectIndex is 2 or 3 || IsMusic && MusicRainbow;
    public bool UsesColor=>EffectIndex is 0 or 3 || IsMusic && !MusicRainbow;
    public bool IsSegmented=>EffectIndex==4;
    public bool IsMusic=>EffectIndex==1;
    public DecorationOptions Options=>new() {Effect=(DecorationEffect)Normalize(EffectIndex),Color=new Rgb(Color.R,Color.G,Color.B),MusicRainbow=MusicRainbow,Speed=Safe(Speed,.1,5,1),Brightness=Safe(Brightness,0,100,100)/100};
    public DecorationSetting Save()=>new() {EffectIndex=Normalize(EffectIndex),ColorHex=$"{Color.R:X2}{Color.G:X2}{Color.B:X2}",MusicRainbow=MusicRainbow,Speed=Options.Speed,Brightness=Options.Brightness*100};
    public void Load(DecorationSetting? value)
    {
        value??=new();_loading=true;
        EffectIndex=Normalize(value.EffectIndex);MusicRainbow=value.MusicRainbow;Speed=Safe(value.Speed,.1,5,1);Brightness=Safe(value.Brightness,0,100,100);
        Color=Avalonia.Media.Color.TryParse("#"+value.ColorHex?.TrimStart('#'),out var color)?color:Avalonia.Media.Color.FromRgb(255,0,60);
        _loading=false;
    }
    private int Normalize(int value)=>Choices.Any(c=>c.Value==value)?value:0;
    private void NotifyOptions()
    {
        OnPropertyChanged(nameof(SelectedChoice));OnPropertyChanged(nameof(IsMusic));OnPropertyChanged(nameof(IsSegmented));
        OnPropertyChanged(nameof(HasSpeed));OnPropertyChanged(nameof(UsesColor));Changed();
    }
    partial void OnEffectIndexChanged(int value) {int valid=Normalize(value);if(valid!=value){EffectIndex=valid;return;}NotifyOptions();}
    partial void OnMusicRainbowChanged(bool value)=>NotifyOptions();
    partial void OnColorChanged(Color value)=>Changed();
    partial void OnSpeedChanged(double value)=>Changed();
    partial void OnBrightnessChanged(double value)=>Changed();
    [RelayCommand] private void Reset() {Load(new DecorationSetting {EffectIndex=EffectIndex});Changed();}
    private void Changed() {if(!_loading)changed();}
    private static double Safe(double v,double min,double max,double fallback)=>double.IsFinite(v)?Math.Clamp(v,min,max):fallback;
}
