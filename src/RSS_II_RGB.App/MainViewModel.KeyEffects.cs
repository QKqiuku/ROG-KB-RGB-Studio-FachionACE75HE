using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.App;

internal sealed partial class MainViewModel
{
    private bool _loadingReactive;
    public PresetItem[] ReactivePresets {get;}
    public EffectChoice[] ReactiveChoices => PresetCatalog.KeyChoices;
    public bool ShowKeyControls => EnableReactive && SelectedReactiveEffect != EffectChoice.Reactive;
    public bool ReactiveHasColor => SelectedReactiveEffect != EffectChoice.KeyDarkRipple;
    public bool ReactiveUsesCustomColor => !ReactivePalette && !ReactiveRainbow;
    [ObservableProperty] private EffectChoice _selectedReactiveEffect=EffectChoice.Reactive;
    [ObservableProperty] private double _reactiveSpeed=1;
    [ObservableProperty] private double _reactiveWidth=1;
    [ObservableProperty] private double _reactiveBrightness=100;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ReactiveUsesCustomColor))] private bool _reactivePalette=true;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ReactiveUsesCustomColor))] private bool _reactiveRainbow;
    [ObservableProperty] private Color _reactiveColor=Color.FromRgb(0,180,255);

    private void ChooseReactivePreset(EffectChoice effect)
    {
        IsCustomMode=true;
        SelectedReactiveEffect=effect;
        EnableReactive=true;
        UpdateReactiveSelection();
    }
    private void UpdateReactiveSelection()
    {
        foreach(var item in ReactivePresets) item.IsSelected=IsCustomMode && EnableReactive && item.Effect==SelectedReactiveEffect;
        OnPropertyChanged(nameof(ShowKeyControls));
        OnPropertyChanged(nameof(ReactiveHasColor));
    }
    private void LoadReactiveSettings()
    {
        _loadingReactive=true;
        var setting=_settings.Settings.EffectSettings.GetValueOrDefault(SelectedReactiveEffect)??new EffectSetting {ColorHex="00B4FF"};
        ReactiveSpeed=Finite(setting.Speed,.1,5,1);
        ReactiveWidth=Finite(setting.Width,.25,3,1);
        ReactiveBrightness=Finite(setting.Brightness,0,100,100);
        ReactiveRainbow=setting.ReactiveRainbow;
        ReactivePalette=setting.Rainbow && !ReactiveRainbow;
        ReactiveColor=TryParseColor(setting.ColorHex,out var color)?color:Color.FromRgb(0,180,255);
        _loadingReactive=false;
    }
    private void ApplyReactiveSettings()
    {
        _controller.ReactiveEffect=SelectedReactiveEffect;
        _controller.ReactiveOptions=new PatternOptions {Speed=ReactiveSpeed,Width=ReactiveWidth,Rainbow=ReactivePalette || ReactiveRainbow,ReactiveRainbow=ReactiveRainbow};
        _controller.ReactiveColor=new Rgb(ReactiveColor.R,ReactiveColor.G,ReactiveColor.B);
        _controller.ReactiveBrightness=ReactiveBrightness/100;
        var settings=_settings.Settings;
        settings.ReactiveEffect=SelectedReactiveEffect;
        if(SelectedReactiveEffect!=EffectChoice.Reactive)
            settings.EffectSettings[SelectedReactiveEffect]=new EffectSetting {Speed=ReactiveSpeed,Width=ReactiveWidth,Brightness=ReactiveBrightness,
                Rainbow=ReactivePalette,ReactiveRainbow=ReactiveRainbow,ColorHex=$"{ReactiveColor.R:X2}{ReactiveColor.G:X2}{ReactiveColor.B:X2}"};
    }
    partial void OnSelectedReactiveEffectChanged(EffectChoice value)
    {
        if(!ReactiveChoices.Contains(value)) {SelectedReactiveEffect=EffectChoice.Reactive;return;}
        if(!_loading) LoadReactiveSettings();
        UpdateReactiveSelection();Apply();
    }
    partial void OnReactiveSpeedChanged(double value) {if(!_loadingReactive) Apply();}
    partial void OnReactiveWidthChanged(double value) {if(!_loadingReactive) Apply();}
    partial void OnReactiveBrightnessChanged(double value) {if(!_loadingReactive) Apply();}
    partial void OnReactivePaletteChanged(bool value)
    {
        if (_loadingReactive) return;
        _loadingReactive=true;
        if(value) ReactiveRainbow=false;
        _loadingReactive=false;Apply();
    }
    partial void OnReactiveRainbowChanged(bool value)
    {
        if (_loadingReactive) return;
        _loadingReactive=true;
        if(value) ReactivePalette=false;
        _loadingReactive=false;Apply();
    }
    partial void OnReactiveColorChanged(Color value)
    {
        if (_loadingReactive) return;
        _loadingReactive=true;ReactivePalette=false;ReactiveRainbow=false;_loadingReactive=false;Apply();
    }
}
