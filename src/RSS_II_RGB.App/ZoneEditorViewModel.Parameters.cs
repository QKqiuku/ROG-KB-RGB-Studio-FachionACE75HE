using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace RSS_II_RGB.App;
internal sealed partial class ZoneEditorViewModel
{
    private bool _loadingZoneParameters;
    [ObservableProperty] private bool _zoneRainbow;
    [ObservableProperty] private double _zoneSpeed=1;
    [ObservableProperty] private double _zoneWidth=1;
    [ObservableProperty] private double _zoneDensity=30;
    [ObservableProperty] private double _zoneDirection;
    [ObservableProperty] private double _zoneSaturation=100;
    [ObservableProperty] private double _zoneBrightness=100;
    [ObservableProperty] private bool _zoneMirrorX;
    [ObservableProperty] private bool _zoneMirrorY;
    [ObservableProperty] private Color _zoneWaveColor1=Color.FromRgb(0,209,255);
    [ObservableProperty] private Color _zoneWaveColor2=Color.FromRgb(255,0,153);
    public string ZoneEffectName=>L.EffectName(ZoneEffect);
    public bool ZoneHasRainbow=>ZoneEffect is not EffectChoice.Off and not EffectChoice.Solid;
    public bool ZoneHasSpeed=>PresetCatalog.HasMotion(ZoneEffect,ZoneRainbow);
    public bool ZoneHasWidth=>PresetCatalog.HasWidth(ZoneEffect);
    public bool ZoneHasDensity=>PresetCatalog.HasDensity(ZoneEffect);
    public bool ZoneHasGeometry=>PresetCatalog.HasGeometry(ZoneEffect);
    public bool ZoneIsSoundWave=>ZoneEffect==EffectChoice.MusicSoundWave;
    public bool ZoneShowSaturation=>ZoneHasRainbow && ZoneRainbow && (PresetCatalog.PatternFor(ZoneEffect) is not null || PresetCatalog.MusicFor(ZoneEffect) is not null);
    private void NotifyZoneParameters()
    {
        foreach(var name in new[]{nameof(ZoneEffectName),nameof(ZoneHasRainbow),nameof(ZoneHasSpeed),nameof(ZoneHasWidth),nameof(ZoneHasDensity),nameof(ZoneHasGeometry),nameof(ZoneIsSoundWave),nameof(ZoneShowSaturation)})OnPropertyChanged(name);
    }
    private void LoadZoneParameters(EffectSetting p)
    {
        bool previous=_loadingZoneParameters;_loadingZoneParameters=true;
        try
        {
            ZoneSpeed=Safe(p.Speed,.1,5,1);ZoneWidth=Safe(p.Width,.25,3,1);ZoneDensity=Safe(p.Density,.05,1,.3)*100;
            ZoneDirection=Safe(p.Direction,0,360,0);ZoneSaturation=Safe(p.Saturation,0,100,100);ZoneBrightness=Safe(p.Brightness,0,100,100);
            ZoneMirrorX=p.MirrorX;ZoneMirrorY=p.MirrorY;ZoneRainbow=p.Rainbow;
            var a=ZoneMapping.ParseRgb(p.WaveColor1Hex);var b=ZoneMapping.ParseRgb(p.WaveColor2Hex);
            ZoneWaveColor1=Color.FromRgb(a.R,a.G,a.B);ZoneWaveColor2=Color.FromRgb(b.R,b.G,b.B);
        }
        finally {_loadingZoneParameters=previous;}
        NotifyZoneParameters();
    }
    private EffectSetting CaptureZoneParameters()=>new()
    {
        Speed=Safe(ZoneSpeed,.1,5,1),Width=Safe(ZoneWidth,.25,3,1),Density=Safe(ZoneDensity,5,100,30)/100,
        Direction=Safe(ZoneDirection,0,360,0),Saturation=Safe(ZoneSaturation,0,100,100),Brightness=Safe(ZoneBrightness,0,100,100),
        MirrorX=ZoneMirrorX,MirrorY=ZoneMirrorY,Rainbow=ZoneRainbow,ColorHex=$"{ZoneColor.R:X2}{ZoneColor.G:X2}{ZoneColor.B:X2}",
        WaveColor1Hex=$"{ZoneWaveColor1.R:X2}{ZoneWaveColor1.G:X2}{ZoneWaveColor1.B:X2}",WaveColor2Hex=$"{ZoneWaveColor2.R:X2}{ZoneWaveColor2.G:X2}{ZoneWaveColor2.B:X2}"
    };
    partial void OnZoneRainbowChanged(bool value)
    {
        NotifyZoneParameters();if(_loadingZoneParameters)return;
        if(IsAudioMode && SelectedAudioMode!=AudioZoneMode.Spectrum)SelectedAudioMode=value?AudioZoneMode.SolidRainbow:AudioZoneMode.SolidColor;
        if(!value && ZoneIsSoundWave)ZoneWaveColor1=ZoneColor;
    }
    partial void OnSelectedAudioModeChanged(AudioZoneMode value)
    {if(!_loadingZoneParameters && IsAudioMode && value!=AudioZoneMode.Spectrum)ZoneRainbow=value==AudioZoneMode.SolidRainbow;}
    partial void OnZoneWaveColor1Changed(Color value)
    {if(!_loadingZoneParameters){ZoneColor=value;ZoneRainbow=false;}}
    partial void OnZoneWaveColor2Changed(Color value)
    {if(!_loadingZoneParameters)ZoneRainbow=false;}
    [RelayCommand] private void ResetZoneParameters()
    {
        bool previous=_loadingZoneParameters;_loadingZoneParameters=true;
        try
        {
            var p=PresetCatalog.DefaultSettings(ZoneEffect);var color=ZoneMapping.ParseRgb(p.ColorHex);
            ZoneColor=Color.FromRgb(color.R,color.G,color.B);ZoneColorHex=$"#{color.R:X2}{color.G:X2}{color.B:X2}";SelectedAudioMode=AudioZoneMode.Spectrum;LoadZoneParameters(p);
        }
        finally {_loadingZoneParameters=previous;}
    }
    private static double Safe(double v,double min,double max,double fallback)=>double.IsFinite(v)?Math.Clamp(v,min,max):fallback;
}
