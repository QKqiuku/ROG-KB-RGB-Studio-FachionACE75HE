using CommunityToolkit.Mvvm.ComponentModel;
using RSS_II_RGB.Core.Engine;
namespace RSS_II_RGB.App;
internal sealed partial class MainViewModel
{
    public DecorationEditorViewModel BarEditor {get;}
    public DecorationEditorViewModel EyeEditor {get;}
    public bool ShowBarEditor=>IsCustomMode && HasDecorativeLighting && LightBarModeIndex==2;
    public bool ShowEyeEditor=>IsCustomMode && HasDecorativeLighting && EyeModeIndex==2;
    public bool OverallAnimated=>OverallModeIndex!=0;
    [ObservableProperty] private int _overallModeIndex;
    [ObservableProperty] private double _overallOpacity=100;
    [ObservableProperty] private double _overallMinimum=10;
    [ObservableProperty] private double _overallSpeed=1;
    private OutputEnvelopeOptions OverallOptions=>new() {Mode=(OutputEnvelopeMode)Math.Clamp(OverallModeIndex,0,2),Opacity=Finite(OverallOpacity,0,100,100)/100,Minimum=Finite(OverallMinimum,0,100,10)/100,Speed=Finite(OverallSpeed,.1,5,1)};
    private void OutputSettingsChanged()
    {
        OnPropertyChanged(nameof(ShowBarEditor));OnPropertyChanged(nameof(ShowEyeEditor));OnPropertyChanged(nameof(ShowMusicControls));
        UpdateStripSelection();Apply();
    }
    private void UpdateStripSelection()
    {if(Editor is not null){Editor.CanSelectDecorations=IsCustomMode;Editor.CanSelectBarSegments=ShowBarEditor && BarEditor.IsSegmented;}}
    private void EnableStripZones()
    {SelectDecoration(false);BarEditor.EffectIndex=4;UpdateStripSelection();}
    private void RestoreOutputSettings()
    {
        var s=_settings.Settings;var o=s.OverallEffect??new();
        OverallModeIndex=Math.Clamp((int)o.Mode,0,2);OverallOpacity=Finite(o.Opacity,0,1,1)*100;
        OverallMinimum=Finite(o.Minimum,0,1,.1)*100;OverallSpeed=Finite(o.Speed,.1,5,1);
        BarEditor.Load(s.BarEffect);EyeEditor.Load(s.EyeEffect);
    }
    private void ApplyOutputSettings()
    {
        _controller.OverallEffect=OverallOptions;_controller.BarEffect=BarEditor.Options;_controller.EyeEffect=EyeEditor.Options;
        UpdateStripSelection();
        var s=_settings.Settings;s.OverallEffect=OverallOptions;s.BarEffect=BarEditor.Save();s.EyeEffect=EyeEditor.Save();
    }
    private void SelectDecoration(bool eye)
    {
        if(!IsCustomMode)return;
        if(eye)EyeModeIndex=2;else LightBarModeIndex=2;
    }
    partial void OnOverallModeIndexChanged(int value) {OnPropertyChanged(nameof(OverallAnimated));OutputSettingsChanged();}
    partial void OnOverallOpacityChanged(double value)=>OutputSettingsChanged();
    partial void OnOverallMinimumChanged(double value)=>OutputSettingsChanged();
    partial void OnOverallSpeedChanged(double value)=>OutputSettingsChanged();
}
