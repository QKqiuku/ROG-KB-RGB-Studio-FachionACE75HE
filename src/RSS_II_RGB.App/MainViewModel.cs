using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Layout;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RSS_II_RGB.Core.Animation;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Startup;
using RSS_II_RGB.Core.Effects.Layers;

namespace RSS_II_RGB.App;

/// <summary>
/// UI state for the main window. Uses CommunityToolkit.Mvvm source generators
/// only (no reflection) so it survives Native AOT trimming.
/// </summary>
internal sealed partial class MainViewModel : ObservableObject
{
    private readonly KeyboardController _controller;
    private readonly SettingsService _settings;
    private readonly IStartupManager _startup;
    private bool _ready;
    private bool _loading;
    private bool _loadingEffect;
    private bool _syncColor;

    public PresetItem[] RegularPresets { get; }
    public PresetItem[] AnimationPresets { get; }
    public IReadOnlyList<int> FrameRateOptions => AppConstants.FrameRateOptions;

    [ObservableProperty] private bool _isCustomMode = true;
    [ObservableProperty] private double _effectSpeed = 1;
    [ObservableProperty] private double _effectWidth = 1;
    [ObservableProperty] private double _effectDensity = 30;
    [ObservableProperty] private double _effectDirection;
    [ObservableProperty] private double _effectSaturation = 100;
    [ObservableProperty] private bool _rainbowColors = true;
    [ObservableProperty] private HsvColor _pickedHsv = Color.FromRgb(0, 255, 102).ToHsv();
    [ObservableProperty] private string _pickedHex = "#00FF66";
    [ObservableProperty] private bool _invalidColor;
    [ObservableProperty] private int _targetFps = AppConstants.Falchion75TargetFps;

    [ObservableProperty] private int _lightBarModeIndex;
    [ObservableProperty] private int _eyeModeIndex;
    public bool HasDecorativeLighting => ReferenceEquals(_controller.Profile, Falchion75Layout.Profile);
    partial void OnLightBarModeIndexChanged(int value)
    {
        int valid = (int)Falchion75Lighting.Normalize((DecorationMode)value);
        if(!_loading && !IsCustomMode && valid==2)valid=1;
        if (value != valid) { LightBarModeIndex = valid; return; }
        OutputSettingsChanged();
    }
    partial void OnEyeModeIndexChanged(int value)
    {
        int valid = (int)Falchion75Lighting.Normalize((DecorationMode)value);
        if(!_loading && !IsCustomMode && valid==2)valid=1;
        if (value != valid) { EyeModeIndex = valid; return; }
        OutputSettingsChanged();
    }

    public string ModeText => IsCustomMode ? L.CustomModeHint : L.PresetModeHint;
    public string CurrentEffectName => SelectedEffect == EffectChoice.Atk && !string.IsNullOrEmpty(_settings.Settings.AtkPresetName)
        ? L.AtkGallery + " · " + _settings.Settings.AtkPresetName : L.EffectName(SelectedEffect);
    public bool HasMotion => PresetCatalog.HasMotion(SelectedEffect,RainbowColors);
    public bool HasDirection => CanTransform;
    public bool HasWidth => PresetCatalog.HasWidth(SelectedEffect);
    public bool HasDensity => PresetCatalog.HasDensity(SelectedEffect);
    public bool HasRainbow => SelectedEffect is not EffectChoice.Off and not EffectChoice.Solid and not EffectChoice.Gif and not EffectChoice.Atk;
    public bool CanPickColor => SelectedEffect is not EffectChoice.Off and not EffectChoice.Gif and not EffectChoice.Atk;
    public bool ShowSaturation => HasRainbow && RainbowColors;
    public bool IsAtkEffect => SelectedEffect == EffectChoice.Atk;
    public bool IsGifEffect => SelectedEffect == EffectChoice.Gif;

    [ObservableProperty]
    private ZoneEditorViewModel? _editor;

    public string DeviceName => _controller.Profile.Name.Replace(" (experimental)", "", StringComparison.Ordinal);
    public IBrush StatusBrush => new SolidColorBrush(Color.Parse(IsConnected ? "#4CE6B5" : _controller.ConnectionError is not null ? "#FFB454" : "#7E8798"));
    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusBrush));
        if (Editor is not null) Editor.CanSelectKeys = value && IsCustomMode;
    }

    [ObservableProperty]
    private string _statusText = L.StatusConnecting;

    /// <summary>Localized, formatted labels for the sliders.</summary>
    public string BrightnessText => string.Format(CultureInfo.InvariantCulture, L.BrightnessFormat, BrightnessPercent);
    public string AudioSensitivityText => string.Format(CultureInfo.InvariantCulture, L.AudioSensitivityFormat, AudioSensitivity);

    [ObservableProperty]
    private bool _isConnected;

    // Launch with Windows, registered to start minimised in the tray. The registry
    // is the source of truth, so this isn't part of the saved settings.
    [ObservableProperty]
    private bool _startWithWindows;

    public bool CanStartWithWindows => !Program.EnableFalchion75;

    [ObservableProperty]
    private EffectChoice _selectedEffect = EffectChoice.Rainbow;

    [ObservableProperty]
    private double _brightnessPercent = 100;

    // Independent display overlays, separate from the base effect (layered on top).
    // Reactive outranks Audio: keypress flares always show, audio sits below them.
    [ObservableProperty]
    private bool _enableReactive;

    [ObservableProperty]
    private bool _enableAudio;

    // Starlight overlay: twinkling stars above the base effect, below the other overlays.
    [ObservableProperty]
    private bool _enableStarlight;

    // Audio visualiser brightness multiplier (1 = raw, higher fills the keyboard more).
    [ObservableProperty]
    private double _audioSensitivity = 0.9;

    // Global Audio overlay: spectrum vs three-region bars, with a per-region multiplier.
    [ObservableProperty]
    private AudioLayoutChoice _audioLayout = AudioLayoutChoice.Spectrum;

    [ObservableProperty]
    private double _audioBarsBassMultiplier = 0.9;

    [ObservableProperty]
    private double _audioBarsMidMultiplier = 1.0;

    [ObservableProperty]
    private double _audioBarsTrebleMultiplier = 2.0;

    public AudioLayoutChoice[] AudioLayoutOptions { get; } =
    {
        AudioLayoutChoice.Spectrum, AudioLayoutChoice.Bars,
    };

    /// <summary>True when the bars layout is selected (gates its multiplier sliders).</summary>
    public bool IsAudioBarsLayout => AudioLayout == AudioLayoutChoice.Bars;

    public string AudioBarsBassText =>
        string.Format(CultureInfo.InvariantCulture, L.AudioBarsBassFormat, AudioBarsBassMultiplier);
    public string AudioBarsMidText =>
        string.Format(CultureInfo.InvariantCulture, L.AudioBarsMidFormat, AudioBarsMidMultiplier);
    public string AudioBarsTrebleText =>
        string.Format(CultureInfo.InvariantCulture, L.AudioBarsTrebleFormat, AudioBarsTrebleMultiplier);

    // System-metric overlay.
    [ObservableProperty]
    private bool _showMetrics;

    [ObservableProperty]
    private MetricLayoutChoice _metricLayout = MetricLayoutChoice.FunctionRow;

    [ObservableProperty]
    private decimal _pct1 = 30;
    [ObservableProperty]
    private decimal _pct2 = 60;
    [ObservableProperty]
    private decimal _pct3 = 90;
    [ObservableProperty]
    private decimal _temp1 = 55;
    [ObservableProperty]
    private decimal _temp2 = 65;
    [ObservableProperty]
    private decimal _temp3 = 75;

    public MetricLayoutChoice[] MetricLayoutOptions { get; } = Program.EnableFalchion75
        ? new[] { MetricLayoutChoice.FunctionRow, MetricLayoutChoice.Diagonal }
        : new[] { MetricLayoutChoice.FunctionRow, MetricLayoutChoice.Numpad, MetricLayoutChoice.Diagonal };

    // Single source of truth for the effect colour (ColorPicker + preset swatches).
    [ObservableProperty]
    private Color _pickedColor = Color.FromRgb(0x00, 0xFF, 0x66);

    // Base effects only. Reactive, Audio and Starlight are independent overlay toggles below.
    public EffectChoice[] Effects => [EffectChoice.Off, .. PresetCatalog.All.Where(e => e != EffectChoice.Off)];

    // GIF animation effect: a one-line status describing the imported animation.
    [ObservableProperty]
    private string _gifStatusText = L.GifNone;
    [ObservableProperty] private string _atkStatusText = L.AtkNone;

    public MainViewModel(KeyboardController controller, SettingsService settings, IStartupManager startup, RSS_II_RGB.Core.Audio.IAudioControl? audioControl = null)
    {
        _audioControl=audioControl;
        _controller = controller;
        _settings = settings;
        _startup = startup;
        BarEditor=new DecorationEditorViewModel(OutputSettingsChanged);EyeEditor=new DecorationEditorViewModel(OutputSettingsChanged,true);
        RegularPresets = PresetCatalog.Regular.Select(e => new PresetItem(e, ChoosePreset)).ToArray();
        AnimationPresets = PresetCatalog.Animations.Select(e => new PresetItem(e, ChoosePreset)).ToArray();
        MusicPresets = PresetCatalog.Music.Select(e => new PresetItem(e, ChoosePreset)).ToArray();
        ReactivePresets = PresetCatalog.KeyEffects.Select(e => new PresetItem(e, ChooseReactivePreset)).ToArray();
    }

    /// <summary>The shared controller, handed to the zone editor.</summary>
    public KeyboardController Controller => _controller;

    /// <summary>The shared settings store, handed to the zone editor.</summary>
    public SettingsService Settings => _settings;

    public Task InitializeAsync()
    {
        RestoreSettings();
        _controller.StateChanged += OnControllerStateChanged;
        StatusText = L.StatusSearching;
        _controller.Start();
        return Task.CompletedTask;
    }

    internal void RestoreSettings(bool preserveAudioSource=false)
    {
        // Restore the saved setup first — it applies whether or not a keyboard is
        // present yet; the controller reconnects on its own and re-applies it.
        _loading = true;
        StartWithWindows = CanStartWithWindows && _startup.IsEnabled();
        AppSettings s = _settings.Settings;
        TargetFps = s.TargetFps ?? (Program.EnableFalchion75 ? AppConstants.Falchion75TargetFps : RSS_II_RGB.Core.CoreConstants.DefaultTargetFps);
        LightBarModeIndex = (int)Falchion75Lighting.Normalize(s.LightBarMode);
        EyeModeIndex = (int)Falchion75Lighting.Normalize(s.EyeMode);
        s.EffectSettings ??= new();
        s.FavoriteEffects ??= new();
        s.RecentEffects ??= new();
        if(s.EffectSettings.Remove(EffectChoice.MulticolorSineSectors,out var oldSectors))
        {
            if(s.GlobalEffect==EffectChoice.MulticolorSineSectors || s.CustomBaseEffect==EffectChoice.MulticolorSineSectors || !s.EffectSettings.ContainsKey(EffectChoice.SineSectors))
                s.EffectSettings[EffectChoice.SineSectors]=oldSectors;
        }
        if(s.GlobalEffect==EffectChoice.MulticolorSineSectors && oldSectors is null)
        {var defaults=PresetCatalog.DefaultSettings(EffectChoice.SineSectors);defaults.Rainbow=true;s.EffectSettings[EffectChoice.SineSectors]=defaults;}
        s.FavoriteEffects=s.FavoriteEffects.Select(e=>e==EffectChoice.MulticolorSineSectors?EffectChoice.SineSectors:e).Distinct().ToList();
        s.RecentEffects=s.RecentEffects.Select(e=>e==EffectChoice.MulticolorSineSectors?EffectChoice.SineSectors:e).Distinct().ToList();
        s.GlobalEffect = PresetCatalog.Available(s.GlobalEffect);
        if (s.CustomBaseEffect is { } savedBase) s.CustomBaseEffect = PresetCatalog.Available(savedBase);
        s.FavoriteEffects.RemoveAll(e => PresetCatalog.IsRemoved(e) || e == EffectChoice.Ripple);
        s.RecentEffects.RemoveAll(e => PresetCatalog.IsRemoved(e) || e == EffectChoice.Ripple);
        foreach (var removed in s.EffectSettings.Keys.Where(PresetCatalog.IsRemoved).ToArray()) s.EffectSettings.Remove(removed);
        foreach (var zone in s.Zones) PresetCatalog.NormalizeZone(zone);
        if (s.GlobalEffect == EffectChoice.Ripple || s.CustomBaseEffect == EffectChoice.Ripple)
        {
            s.EffectSettings.TryAdd(EffectChoice.Ripple,new EffectSetting {ColorHex=s.GlobalColorHex,Brightness=s.BrightnessPercent,Rainbow=false});
            if(s.GlobalEffect == EffectChoice.Ripple) {s.GlobalEffect=EffectChoice.Off;s.UseCustomLayers=true;}
            s.EnableReactive=true;s.ReactiveEffect=EffectChoice.Ripple;
        }
        if (s.CustomBaseEffect == EffectChoice.Ripple) s.CustomBaseEffect=EffectChoice.Off;
        IsCustomMode = s.UseCustomLayers;
        if(!IsCustomMode) {if(LightBarModeIndex==2)LightBarModeIndex=1;if(EyeModeIndex==2)EyeModeIndex=1;}
        SelectedReactiveEffect = ReactiveChoices.Contains(s.ReactiveEffect) ? s.ReactiveEffect : EffectChoice.Reactive;
        EnableReactive = s.EnableReactive;
        EnableAudio = s.EnableAudio;
        EnableStarlight = s.EnableStarlight;
        // Reactive/Audio/Starlight are now overlay toggles, and CpuTemp/GpuTemp are no
        // longer base effects. Migrate any of those legacy choices to a base + the toggle.
        switch (s.GlobalEffect)
        {
            case EffectChoice.Reactive:
                SelectedEffect = EffectChoice.Off;
                EnableReactive = true;
                break;
            case EffectChoice.Audio:
                SelectedEffect = EffectChoice.Off;
                EnableAudio = true;
                break;
            case EffectChoice.Starlight:
                // Old "Starlight base" = stars over black: base Off + the overlay reproduces it.
                SelectedEffect = EffectChoice.Off;
                EnableStarlight = true;
                break;
            case EffectChoice.CpuTemp or EffectChoice.GpuTemp:
                SelectedEffect = EffectChoice.Rainbow;
                break;
            default:
                SelectedEffect = s.GlobalEffect;
                break;
        }
        if (TryParseColor(s.GlobalColorHex, out Color color))
        {
            PickedColor = color;
        }
        BrightnessPercent = s.BrightnessPercent;
        AudioSensitivity = s.AudioSensitivity;
        AudioLayout = s.AudioLayout;
        AudioBarsBassMultiplier = s.AudioBarsBassMultiplier;
        AudioBarsMidMultiplier = s.AudioBarsMidMultiplier;
        AudioBarsTrebleMultiplier = s.AudioBarsTrebleMultiplier;
        ShowMetrics = s.ShowMetrics;
        MetricLayout = s.MetricLayout;
        if (s.PercentThresholds.Length >= 3)
        {
            Pct1 = (decimal)s.PercentThresholds[0];
            Pct2 = (decimal)s.PercentThresholds[1];
            Pct3 = (decimal)s.PercentThresholds[2];
        }
        if (s.TempThresholds.Length >= 3)
        {
            Temp1 = (decimal)s.TempThresholds[0];
            Temp2 = (decimal)s.TempThresholds[1];
            Temp3 = (decimal)s.TempThresholds[2];
        }
        LoadGifAnimation(s);
        LoadAtkAnimation(s);
        LoadEffectSettings(SelectedEffect);
        LoadReactiveSettings();
        RestoreMusicSettings(preserveAudioSource);
        RestoreOutputSettings();
        _loading = false;
        _ready = true;

        // Push the restored configuration to the controller, then begin the
        // detect/reconnect lifecycle; the status text tracks StateChanged.
        _controller.SetZones(s.Zones.Select(ZoneMapping.ToZone).ToArray(),rebuild:false);
        Editor = new ZoneEditorViewModel(_controller, _settings);
        Editor.DecorationSelected=SelectDecoration;
        Editor.StripZonesRequested=EnableStripZones;
        Editor.ZonesChanged=OnZoneEffectsChanged;
        RestoreUserPresetLibrary();
        UpdatePresetSelection();
        Apply();

    }

    private void OnControllerStateChanged() => Dispatcher.UIThread.Post(() =>
    {
        IsConnected = _controller.IsConnected;
        OnPropertyChanged(nameof(StatusBrush));
        if (IsConnected) _controller.SetTargetFps(TargetFps);
        StatusText = _controller.IsConnected
            ? string.Format(CultureInfo.InvariantCulture, L.StatusConnectedFormat, _controller.Profile.Name, _controller.Firmware)
            : ConnectionFailureText(_controller.ConnectionError);
    });

    [RelayCommand]
    private void SetColor(string hex)
    {
        if (TryParseColor(hex, out Color color))
        {
            if (HasRainbow) RainbowColors = false;
            PickedColor = color;
        }
    }

    // Restore a previously imported GIF animation so EffectChoice.Gif can play it.
    private void LoadGifAnimation(AppSettings s)
    {
        if (string.IsNullOrEmpty(s.GifAnimPath) || !File.Exists(s.GifAnimPath))
        {
            GifStatusText = L.GifNone;
            return;
        }
        try
        {
            KbAnim anim = KbAnim.Load(s.GifAnimPath);
            _controller.SetGifAnimation(anim,rebuild:!_loading);
            string name = string.IsNullOrEmpty(s.GifSourcePath) ? "" : Path.GetFileName(s.GifSourcePath);
            GifStatusText = string.Format(CultureInfo.InvariantCulture, L.GifImportedFormat, name, anim.Frames.Count);
        }
        catch
        {
            GifStatusText = L.GifNone;
        }
    }

    /// <summary>Pick a GIF, crop it, bake it, and switch the base effect to it.</summary>
    public bool CanImportGif=>GifImportService.LocateHelper() is not null;
    public string GifImportHint=>CanImportGif?L.GifImportButton:L.GifComponentMissing;

    public async Task ImportGifAsync(Window owner)
    {
        AppSettings s = _settings.Settings;
        GifImportOutcome outcome = await GifImportService.ImportAsync(
            owner, _controller.Profile, s.GifSourcePath, s.GifCrop, status => GifStatusText = status);
        if (outcome.Cancelled)
        {
            return;
        }
        if (!outcome.Success)
        {
            GifStatusText = string.Format(CultureInfo.InvariantCulture, L.GifImportFailedFormat, outcome.Error);
            return;
        }

        try
        {
            KbAnim anim = KbAnim.Load(outcome.AnimPath!);
            _controller.SetGifAnimation(anim,rebuild:!_loading);
            s.GifAnimPath = outcome.AnimPath;
            s.GifSourcePath = outcome.SourcePath;
            s.GifCrop = outcome.Crop;
            GifStatusText = string.Format(CultureInfo.InvariantCulture, L.GifImportedFormat,
                Path.GetFileName(outcome.SourcePath ?? ""), outcome.FrameCount) + L.GifReadyNote;
            IsCustomMode = false;
            SelectedEffect = EffectChoice.Gif;
            _settings.Save();
        }
        catch (Exception ex)
        {
            GifStatusText = string.Format(CultureInfo.InvariantCulture, L.GifImportFailedFormat, ex.Message);
        }
    }

    private void LoadAtkAnimation(AppSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AtkAnimPath)) return;
        try
        {
            if (new FileInfo(settings.AtkAnimPath).Length > AppConstants.AtkMaxResponseBytes) throw new InvalidDataException("Cached scene is too large.");
            using var json = JsonDocument.Parse(File.ReadAllText(settings.AtkAnimPath), new JsonDocumentOptions { MaxDepth = 32 });
            _controller.SetAtkAnimation(AtkScene.Parse(json.RootElement, _controller.Profile, settings.AtkFitLayout),rebuild:!_loading);
            AtkStatusText = $"{settings.AtkPresetName} · {settings.AtkPresetAuthor} · ATK #{settings.AtkPresetId}";
        }
        catch { AtkStatusText = L.AtkLoadFailed; }
    }

    internal async Task ImportAtkAsync(AtkPreset preset, AtkScene scene, bool fit, string? cacheDirectory = null)
    {
        if (!AtkLayoutFilter.Matches(preset, _controller.Profile)) throw new InvalidDataException("Source layout does not match the active keyboard.");
        string directory = cacheDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.AppFolderName, "anims");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"atk-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, preset.Scene.GetRawText());
        var settings = _settings.Settings;
        settings.AtkAnimPath = path;
        settings.AtkPresetName = preset.Title;
        settings.AtkPresetAuthor = preset.Author;
        settings.AtkPresetId = preset.Id;
        settings.AtkFitLayout = fit;
        _controller.SetAtkAnimation(scene);
        AtkStatusText = $"{preset.Title} · {preset.Author} · ATK #{preset.Id}";
        IsCustomMode = false;
        SelectedEffect = EffectChoice.Atk;
        EffectSpeed = 1;
        BrightnessPercent = 100;
        OnPropertyChanged(nameof(CurrentEffectName));
        Apply();
    }

    private void ChoosePreset(EffectChoice effect)
    {
        IsCustomMode = false;
        SelectedEffect = effect;
        UpdatePresetSelection();
    }

    [RelayCommand] private void ChooseCustom() => IsCustomMode = true;

    private void UpdatePresetSelection()
    {
        foreach (var item in RegularPresets.Concat(AnimationPresets).Concat(MusicPresets)) item.IsSelected = !IsCustomMode && item.Effect == SelectedEffect;
        UpdateReactiveSelection();
        foreach(var item in UserPresets)item.IsSelected=IsCustomMode && item.Id==_activeUserPresetId;
    }

    partial void OnIsCustomModeChanged(bool value)
    {
        if(!_loading)
        {
            bool previous=_loadingEffect;_loadingEffect=true;
            try
            {
                if(value)
                {
                    _settings.Settings.CustomBaseEffect=SelectedEffect;
                    Editor?.ClearZonesCommand.Execute(null);
                    EnableReactive=false;EnableAudio=false;EnableStarlight=false;ShowMetrics=false;
                    OverallModeIndex=0;OverallOpacity=100;OverallMinimum=10;OverallSpeed=1;
                }
                else
                {
                    ForgetUserPreset();
                    _settings.Settings.CustomBaseEffect=SelectedEffect;
                    LightBarModeIndex=1;EyeModeIndex=1;
                }
            }
            finally {_loadingEffect=previous;}
        }
        OnPropertyChanged(nameof(ModeText));
        OnPropertyChanged(nameof(ShowBarEditor));OnPropertyChanged(nameof(ShowEyeEditor));OnPropertyChanged(nameof(ShowMusicControls));
        if(Editor is not null)Editor.CanSelectKeys=value && IsConnected;
        UpdatePresetSelection();Apply();
    }

    partial void OnSelectedEffectChanged(EffectChoice value)
    {
        if (value == EffectChoice.Ripple) { SelectedEffect=EffectChoice.Off;ChooseReactivePreset(EffectChoice.Ripple);return; }
        if (PresetCatalog.IsRemoved(value)) { SelectedEffect = PresetCatalog.Available(value); return; }
        RememberPreset(value);
        RefreshLibrary();
        LoadEffectSettings(value);
        UpdatePresetSelection();
        foreach (string property in new[] { nameof(CurrentEffectName), nameof(HasMotion), nameof(HasDirection), nameof(HasWidth), nameof(HasDensity), nameof(HasRainbow), nameof(CanPickColor), nameof(IsGifEffect), nameof(ShowSaturation), nameof(IsAtkEffect), nameof(IsMusicEffect), nameof(ShowMusicControls), nameof(MusicCanvasHintText), nameof(IsSoundWave), nameof(ShowSingleColor) })
            OnPropertyChanged(property);
        Apply();
    }

    private void LoadEffectSettings(EffectChoice effect)
    {
        _loadingEffect = true;
        var settings = _settings.Settings;
        var option = settings.EffectSettings.GetValueOrDefault(effect) ?? new EffectSetting
        {
            Rainbow = PresetCatalog.DefaultRainbow(effect), ColorHex = settings.GlobalColorHex,
            Brightness = settings.BrightnessPercent
        };
        EffectSpeed = Finite(option.Speed, 0.1, 5, 1);
        MirrorX = option.MirrorX;
        MirrorY = option.MirrorY;
        EffectWidth = Finite(option.Width, 0.25, 3, 1);
        EffectDensity = Finite(option.Density * 100, 5, 100, 30);
        EffectDirection = Finite(option.Direction, 0, 360, 0);
        EffectSaturation = Finite(option.Saturation, 0, 100, 100);
        RainbowColors = option.Rainbow;
        BrightnessPercent = Finite(option.Brightness, 0, 100, 100);
        if (TryParseColor(option.ColorHex, out var color)) PickedColor = color;
        PickedHex = $"#{PickedColor.R:X2}{PickedColor.G:X2}{PickedColor.B:X2}";
        WaveColor1 = TryParseColor(option.WaveColor1Hex,out var wave1)?wave1:Color.FromRgb(0,209,255);
        WaveColor2 = TryParseColor(option.WaveColor2Hex,out var wave2)?wave2:Color.FromRgb(255,0,153);
        _loadingEffect = false;
    }

    private static double Finite(double value, double min, double max, double fallback) => double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    partial void OnTargetFpsChanged(int value)
    {
        int valid = AppConstants.NormalizeTargetFps(value);
        if (value != valid) { TargetFps = valid; return; }
        Apply();
    }
    partial void OnEffectSpeedChanged(double value) => Apply();
    partial void OnEffectWidthChanged(double value) => Apply();
    partial void OnEffectDensityChanged(double value) => Apply();
    partial void OnEffectDirectionChanged(double value) => Apply();
    partial void OnEffectSaturationChanged(double value) => Apply();
    partial void OnRainbowColorsChanged(bool value)
    {
        OnPropertyChanged(nameof(CanPickColor));
        OnPropertyChanged(nameof(ShowSaturation));
        OnPropertyChanged(nameof(HasMotion));
        Apply();
    }

    partial void OnPickedHsvChanged(HsvColor value)
    {
        if (_syncColor) return;
        _syncColor = true;
        PickedColor = value.ToRgb();
        _syncColor = false;
    }

    partial void OnPickedHexChanged(string value)
    {
        InvalidColor = !TryParseColor(value, out var color);
        if (!InvalidColor && color != PickedColor) PickedColor = color;
    }

    partial void OnEnableReactiveChanged(bool value) { UpdateReactiveSelection(); Apply(); }

    partial void OnEnableAudioChanged(bool value) => Apply();

    partial void OnEnableStarlightChanged(bool value) => Apply();

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_loading || !CanStartWithWindows)
        {
            return;
        }
        _startup.SetEnabled(value);
        // Reflect the real registry state in case the write was blocked.
        bool actual = _startup.IsEnabled();
        if (actual != value)
        {
            StartWithWindows = actual;
        }
    }

    partial void OnBrightnessPercentChanged(double value)
    {
        OnPropertyChanged(nameof(BrightnessText));
        Apply();
    }

    partial void OnPickedColorChanged(Color value)
    {
        if (_ready && !_loading && !_loadingEffect && HasRainbow) RainbowColors = false;
        if (!_syncColor)
        {
            _syncColor = true;
            PickedHsv = value.ToHsv();
            _syncColor = false;
        }
        PickedHex = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
        Apply();
    }

    partial void OnAudioSensitivityChanged(double value)
    {
        OnPropertyChanged(nameof(AudioSensitivityText));
        Apply();
    }

    partial void OnAudioLayoutChanged(AudioLayoutChoice value)
    {
        OnPropertyChanged(nameof(IsAudioBarsLayout));
        Apply();
    }

    partial void OnAudioBarsBassMultiplierChanged(double value)
    {
        OnPropertyChanged(nameof(AudioBarsBassText));
        Apply();
    }

    partial void OnAudioBarsMidMultiplierChanged(double value)
    {
        OnPropertyChanged(nameof(AudioBarsMidText));
        Apply();
    }

    partial void OnAudioBarsTrebleMultiplierChanged(double value)
    {
        OnPropertyChanged(nameof(AudioBarsTrebleText));
        Apply();
    }

    partial void OnShowMetricsChanged(bool value) => Apply();
    partial void OnMetricLayoutChanged(MetricLayoutChoice value) => Apply();
    partial void OnPct1Changed(decimal value) => Apply();
    partial void OnPct2Changed(decimal value) => Apply();
    partial void OnPct3Changed(decimal value) => Apply();
    partial void OnTemp1Changed(decimal value) => Apply();
    partial void OnTemp2Changed(decimal value) => Apply();
    partial void OnTemp3Changed(decimal value) => Apply();

    private void Apply()
    {
        if (!_ready || _loading || _loadingEffect)
        {
            return;
        }

        double[] percent = { (double)Pct1, (double)Pct2, (double)Pct3 };
        double[] temp = { (double)Temp1, (double)Temp2, (double)Temp3 };

        _controller.EnableReactive = EnableReactive;
        ApplyReactiveSettings();
        _controller.SetTargetFps(TargetFps);
        _controller.LightBarMode = (DecorationMode)LightBarModeIndex;
        _controller.EyeMode = (DecorationMode)EyeModeIndex;
        _controller.EnableCustomLayers = IsCustomMode;
        ApplyOutputSettings();
        _controller.BaseOptions = new PatternOptions { Speed = EffectSpeed, Width = EffectWidth, MirrorX = MirrorX, MirrorY = MirrorY,
            Density = EffectDensity / 100, Direction = EffectDirection, Saturation = EffectSaturation / 100, Rainbow = RainbowColors,
            WaveColor1 = new Rgb(WaveColor1.R,WaveColor1.G,WaveColor1.B), WaveColor2 = new Rgb(WaveColor2.R,WaveColor2.G,WaveColor2.B) };
        _controller.EnableAudio = EnableAudio;
        _controller.EnableStarlight = EnableStarlight;
        _controller.AudioSensitivity = AudioSensitivity;
        _controller.AudioLayout = AudioLayout;
        _controller.AudioBarsBassMultiplier = AudioBarsBassMultiplier;
        _controller.AudioBarsMidMultiplier = AudioBarsMidMultiplier;
        _controller.AudioBarsTrebleMultiplier = AudioBarsTrebleMultiplier;
        _controller.ShowMetrics = ShowMetrics;
        _controller.MetricLayout = MetricLayout;
        _controller.PercentThresholds = percent;
        _controller.TempThresholds = temp;

        var rgb = new Rgb(PickedColor.R, PickedColor.G, PickedColor.B);
        _controller.SetGlobalEffect(SelectedEffect, rgb, BrightnessPercent / 100.0);

        AppSettings s = _settings.Settings;
        s.GlobalEffect = SelectedEffect;
        s.GlobalColorHex = $"{PickedColor.R:X2}{PickedColor.G:X2}{PickedColor.B:X2}";
        s.BrightnessPercent = BrightnessPercent;
        s.TargetFps = TargetFps;
        s.LightBarMode = (DecorationMode)LightBarModeIndex;
        s.EyeMode = (DecorationMode)EyeModeIndex;
        s.UseCustomLayers = IsCustomMode;
        if (IsCustomMode) s.CustomBaseEffect = SelectedEffect;
        s.EffectSettings[SelectedEffect] = new EffectSetting { Speed = EffectSpeed, Width = EffectWidth, MirrorX = MirrorX, MirrorY = MirrorY,
            Density = EffectDensity / 100, Direction = EffectDirection, Saturation = EffectSaturation,
            Rainbow = RainbowColors, ColorHex = s.GlobalColorHex, Brightness = BrightnessPercent,
            WaveColor1Hex = $"{WaveColor1.R:X2}{WaveColor1.G:X2}{WaveColor1.B:X2}", WaveColor2Hex = $"{WaveColor2.R:X2}{WaveColor2.G:X2}{WaveColor2.B:X2}" };
        s.EnableReactive = EnableReactive;
        s.EnableAudio = EnableAudio;
        s.EnableStarlight = EnableStarlight;
        s.AudioSensitivity = AudioSensitivity;
        s.AudioLayout = AudioLayout;
        s.AudioBarsBassMultiplier = AudioBarsBassMultiplier;
        s.AudioBarsMidMultiplier = AudioBarsMidMultiplier;
        s.AudioBarsTrebleMultiplier = AudioBarsTrebleMultiplier;
        s.ShowMetrics = ShowMetrics;
        s.MetricLayout = MetricLayout;
        s.PercentThresholds = percent;
        s.TempThresholds = temp;
        SaveMusicSettings();
        _settings.Save();
        QueueAudioConfiguration();
        OnPropertyChanged(nameof(ShowMusicControls));
    }

    private static bool TryParseColor(string s, out Color color)
    {
        s = (s ?? "").Trim().TrimStart('#');
        if (s.Length == 6 && uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
        {
            color = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
            return true;
        }
        color = Colors.White;
        return false;
    }
}
