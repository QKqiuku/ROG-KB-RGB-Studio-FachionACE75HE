using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RSS_II_RGB.App;

internal sealed partial class MainViewModel
{
    public PresetItem[] FavoritePresets => RegularPresets.Concat(AnimationPresets).Concat(MusicPresets).Where(p => _settings.Settings.FavoriteEffects.Contains(p.Effect)).ToArray();
    public PresetItem[] RecentPresets => _settings.Settings.RecentEffects.Select(e => RegularPresets.Concat(AnimationPresets).Concat(MusicPresets).FirstOrDefault(p => p.Effect == e)).OfType<PresetItem>().ToArray();
    public string FavoriteAction => _settings.Settings.FavoriteEffects.Contains(SelectedEffect) ? L.Unfavorite : L.Favorite;
    public bool CanFavorite => SelectedEffect is not EffectChoice.Atk and not EffectChoice.Gif;
    [ObservableProperty] private bool _mirrorX;
    [ObservableProperty] private bool _mirrorY;
    public bool CanTransform => PresetCatalog.HasGeometry(SelectedEffect);
    [RelayCommand] private void ToggleFavorite()
    {
        if (!CanFavorite) return;
        var favorites = _settings.Settings.FavoriteEffects;
        if (!favorites.Remove(SelectedEffect)) favorites.Add(SelectedEffect);
        _settings.Save(); RefreshLibrary();
    }
    [RelayCommand] private void RandomPreset()
    {
        var choices = PresetCatalog.All.Where(e => e != SelectedEffect && e is not EffectChoice.Off and not EffectChoice.Atk and not EffectChoice.Gif).ToArray();
        ChoosePreset(choices[Random.Shared.Next(choices.Length)]);
    }
    [RelayCommand] private void RotatePattern() => EffectDirection = (EffectDirection + 90) % 360;
    private void RefreshLibrary()
    {
        foreach (var property in new[] { nameof(FavoritePresets), nameof(RecentPresets), nameof(FavoriteAction), nameof(CanFavorite), nameof(CanTransform) }) OnPropertyChanged(property);
    }
    private void RememberPreset(EffectChoice effect)
    {
        if (!_ready || _loading || effect is EffectChoice.Atk or EffectChoice.Gif) return;
        var recent = _settings.Settings.RecentEffects;
        recent.RemoveAll(e => e == effect); recent.Insert(0, effect);
        if (recent.Count > 8) recent.RemoveRange(8, recent.Count - 8);
        RefreshLibrary();
    }
    partial void OnMirrorXChanged(bool value) => Apply();
    partial void OnMirrorYChanged(bool value) => Apply();
}
