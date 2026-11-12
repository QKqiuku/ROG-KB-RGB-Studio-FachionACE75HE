using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RSS_II_RGB.App;

internal sealed partial class MainViewModel
{
    public bool IsSoundWave => SelectedEffect==EffectChoice.MusicSoundWave;
    public bool ShowSingleColor => CanPickColor && !IsSoundWave;
    [ObservableProperty] private Color _waveColor1=Color.FromRgb(0,209,255);
    [ObservableProperty] private Color _waveColor2=Color.FromRgb(255,0,153);

    partial void OnWaveColor1Changed(Color value)=>ApplyWaveColors();
    partial void OnWaveColor2Changed(Color value)=>ApplyWaveColors();
    private void ApplyWaveColors()
    {
        if(_ready && !_loading && !_loadingEffect && IsSoundWave)RainbowColors=false;
        Apply();
    }
}
