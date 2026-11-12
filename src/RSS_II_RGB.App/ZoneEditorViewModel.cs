using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.App;

internal sealed partial class ZoneRowVM : ObservableObject
{
    private readonly Action<ZoneRowVM> _remove;
    private readonly Action<ZoneRowVM> _edit;
    public string Summary { get; }
    public IBrush Swatch { get; }
    [ObservableProperty] private bool _isEditing;

    public ZoneRowVM(string summary, Rgb color, Action<ZoneRowVM> remove, Action<ZoneRowVM> edit)
    {
        Summary = summary;
        Swatch = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        _remove = remove;
        _edit = edit;
    }
    [RelayCommand] private void Remove() => _remove(this);
    [RelayCommand] private void Edit() => _edit(this);
}

/// <summary>Selection and persisted zone editing shared by the workbench and dialog.</summary>
internal sealed partial class ZoneEditorViewModel : ObservableObject
{
    public const double EdgePad = 18;
    private readonly KeyboardController _controller;
    private readonly SettingsService _settings;
    private readonly List<Zone> _zones = new();
    private ZoneRowVM? _editing;
    private ReadOnlyMemory<Rgb> _previewFrame;
    private bool _synchronizingColor;
    private long _sampleTime = Stopwatch.GetTimestamp();
    private long _sampleFrames;

    public ObservableCollection<KeyVM> StripZoneSegments {get;}=new();
    public Action? StripZonesRequested {get;set;}
    [ObservableProperty] private bool _canSelectBarSegments;
    private IEnumerable<KeyVM> ZoneTargets=>Keys.Concat(StripZoneSegments);
    partial void OnCanSelectBarSegmentsChanged(bool value)
    {if(!value)foreach(var segment in StripZoneSegments)segment.IsSelected=false;}
    [ObservableProperty] private bool _canSelectDecorations=true;
    partial void OnCanSelectDecorationsChanged(bool value) {if(!value)foreach(var segment in LightBarSegments)segment.IsSelected=false;}
    public Action<bool>? DecorationSelected {get;set;}
    public void SelectDecoration(bool eye)
    {
        if(!CanSelectDecorations)return;
        foreach(var segment in LightBarSegments)segment.IsSelected=segment.IsRogEye==eye;
        DecorationSelected?.Invoke(eye);
    }
    public ObservableCollection<KeyVM> Keys { get; } = new();
    public ObservableCollection<KeyVM> LightBarSegments { get; } = new();
    public ObservableCollection<ZoneRowVM> ZoneRows { get; } = new();
    public EffectChoice[] ZoneEffects { get; } =
        [.. PresetCatalog.Regular, ..PresetCatalog.Animations, ..PresetCatalog.Music, EffectChoice.Starlight, EffectChoice.Audio];
    public AudioZoneMode[] AudioModeOptions { get; } =
    {
        AudioZoneMode.Spectrum, AudioZoneMode.SolidColor, AudioZoneMode.SolidRainbow,
    };

    [ObservableProperty] private EffectChoice _zoneEffect = EffectChoice.Solid;
    [ObservableProperty] private AudioZoneMode _selectedAudioMode = AudioZoneMode.Spectrum;
    [ObservableProperty] private Color _zoneColor = Color.FromRgb(0x4C, 0xE6, 0xB5);
    [ObservableProperty] private HsvColor _zoneHsvColor = Color.FromRgb(0x4C, 0xE6, 0xB5).ToHsv();
    [ObservableProperty] private string _zoneColorHex = "#4CE6B5";
    [ObservableProperty] private string _colorError = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewStatus))]
    private bool _hasLivePreview;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewStatus))]
    private double _actualFps;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAssign))]
    [NotifyCanExecuteChangedFor(nameof(AssignZoneCommand))]
    private bool _canSelectKeys = true;

    public bool IsAudioMode => ZoneEffect == EffectChoice.Audio;
    public string PreviewStatus => HasLivePreview ? string.Format(CultureInfo.InvariantCulture, L.LiveFpsFormat, ActualFps) : L.PreviewWaiting;
    public bool HasColorError => ColorError.Length != 0;
    public int SelectedCount => ZoneTargets.Count(k => k.IsSelected);
    public KeyVM[] SelectedKeys => ZoneTargets.Where(k => k.IsSelected).Take(12).ToArray();
    public string SelectionText => string.Format(CultureInfo.InvariantCulture, L.SelectedLightCountFormat, SelectedCount);
    public bool CanAssign => CanSelectKeys && SelectedCount > 0 && !HasColorError;
    public bool HasZones => ZoneRows.Count > 0;
    public bool HasNoZones => !HasZones;
    public bool IsEditing => _editing is not null;
    public string ApplyText => IsEditing ? L.UpdateZone : L.AssignToSelection;
    public bool HasLightBar => ReferenceEquals(_controller.Profile, Falchion75Layout.Profile);
    public double CanvasWidth => KeyboardGeometry.Width(_controller.Profile);
    public double CanvasHeight => KeyboardGeometry.Height(_controller.Profile);
    public double HostWidth => CanvasWidth + EdgePad * 2;
    public double HostHeight => CanvasHeight + EdgePad * 2;
    public Thickness KeysMargin => new(EdgePad);

    public ZoneEditorViewModel(KeyboardController controller, SettingsService settings)
    {
        _controller = controller;
        _settings = settings;
        foreach (LedKey key in controller.Profile.Keys)
        {
            var box = KeyboardGeometry.ForKey(controller.Profile, key);
            string? label = HasLightBar && key.Name == "RIGHT_CONTROL" ? "Copilot" : null;
            var vm = new KeyVM(key.Index, key.Name, box.X, box.Y, box.Width, box.Height, label);
            vm.PropertyChanged += OnKeyChanged;
            Keys.Add(vm);
        }
        if (HasLightBar)
        {
            for (int i = 0; i < Falchion75Layout.LightBarLedCount; i++)
                LightBarSegments.Add(new KeyVM(Falchion75Layout.LightBarSourceIndices[i],
                    i == 12 ? AppConstants.RogEyePreviewKeyName : $"ROG bar {i + 1}", 0, 0, label: ""));
        }
        if(HasLightBar)
            for(int i=0;i<12;i++)
            {
                var segment=new KeyVM(83+i,$"Strip {i+1}",0,0,label:(i+1).ToString(CultureInfo.InvariantCulture));
                segment.PropertyChanged+=OnKeyChanged;StripZoneSegments.Add(segment);
            }
        foreach (Zone zone in controller.Zones)
        {
            _zones.Add(zone);
            ZoneRows.Add(CreateRow(zone));
        }
        ZoneRows.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasZones));
            OnPropertyChanged(nameof(HasNoZones));
        };
        RefreshPreview();
    }

    partial void OnZoneEffectChanged(EffectChoice value)
    {
        OnPropertyChanged(nameof(IsAudioMode));
        if(!_loadingZoneParameters){SelectedAudioMode=AudioZoneMode.Spectrum;LoadZoneParameters(PresetCatalog.DefaultSettings(value));}
        NotifyZoneParameters();
    }
    partial void OnCanSelectKeysChanged(bool value) { if (!value) SelectNone(); }
    partial void OnZoneColorChanged(Color value)
    {
        if(!_loadingZoneParameters){ZoneRainbow=false;if(ZoneIsSoundWave)ZoneWaveColor1=value;}
        if (!_synchronizingColor)
        {
            _synchronizingColor = true;
            ZoneHsvColor = value.ToHsv();
            _synchronizingColor = false;
        }
        ZoneColorHex = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
    }
    partial void OnZoneHsvColorChanged(HsvColor value)
    {
        if (_synchronizingColor) return;
        _synchronizingColor = true;
        ZoneColor = value.ToRgb();
        _synchronizingColor = false;
    }
    partial void OnZoneColorHexChanged(string value)
    {
        string hex = (value ?? "").Trim().TrimStart('#');
        if (hex.Length == 6 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
        {
            ColorError = "";
            ZoneColor = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }
        else ColorError = L.InvalidHexColor;
    }
    partial void OnColorErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasColorError));
        OnPropertyChanged(nameof(CanAssign));
        AssignZoneCommand.NotifyCanExecuteChanged();
    }

    private void OnKeyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(KeyVM.IsSelected)) return;
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedKeys));
        OnPropertyChanged(nameof(SelectionText));
        OnPropertyChanged(nameof(CanAssign));
        AssignZoneCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAssign))]
    private void AssignZone()
    {
        if (!CanAssign) return;
        var zone = new Zone(ZoneTargets.Where(k => k.IsSelected).Select(k => k.Index).ToArray(), ZoneEffect,
            new Rgb(ZoneColor.R, ZoneColor.G, ZoneColor.B), SelectedAudioMode,CaptureZoneParameters());
        int index = _editing is null ? -1 : ZoneRows.IndexOf(_editing);
        if (index >= 0)
        {
            _zones[index] = zone;
            ZoneRows[index] = CreateRow(zone);
        }
        else
        {
            _zones.Add(zone);
            ZoneRows.Add(CreateRow(zone));
        }
        Push();
        SelectNone();
    }

    [RelayCommand]
    private void ClearZones()
    {
        _zones.Clear();
        ZoneRows.Clear();
        Push();
        SelectNone();
    }
    [RelayCommand] private void SelectAll() { foreach (var key in Keys) key.IsSelected = true; }
    [RelayCommand]
    private void SelectNone()
    {
        foreach (var key in ZoneTargets) key.IsSelected = false;
        SetEditing(null);
    }

    [RelayCommand]
    private void SetColor(string hex) => ZoneColorHex = hex;

    private ZoneRowVM CreateRow(Zone zone)
    {
        string label = zone.Effect == EffectChoice.Audio
            ? $"{L.EffectName(zone.Effect)} · {L.AudioModeName(zone.AudioMode)}" : L.EffectName(zone.Effect);
        string keys = string.Join(" ",zone.KeyIndices.Take(4).Select(i=>i>=83 && i<95 && HasLightBar?$"L{i-82}":Keys.FirstOrDefault(k=>k.Index==i)?.ShortLabel??"?"));
        return new ZoneRowVM($"{keys} · {L.ZoneSummary(label, zone.KeyIndices.Count)}", zone.Color, RemoveRow, EditRow);
    }
    private void SetEditing(ZoneRowVM? row)
    {
        if (_editing is not null) _editing.IsEditing = false;
        _editing = row;
        if (row is not null) row.IsEditing = true;
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(ApplyText));
    }
    private void EditRow(ZoneRowVM row)
    {
        int index = ZoneRows.IndexOf(row);
        if (index < 0) return;
        Zone zone = _zones[index];
        if(HasLightBar && zone.KeyIndices.Any(i=>i>=83 && i<95))StripZonesRequested?.Invoke();
        SetEditing(row);
        foreach (var key in ZoneTargets) key.IsSelected = zone.KeyIndices.Contains(key.Index);
        _loadingZoneParameters=true;
        try
        {
            ZoneEffect=zone.Effect;SelectedAudioMode=zone.AudioMode;
            ZoneColor=Color.FromRgb(zone.Color.R,zone.Color.G,zone.Color.B);
            ZoneColorHex=$"#{zone.Color.R:X2}{zone.Color.G:X2}{zone.Color.B:X2}";
            LoadZoneParameters(zone.Parameters??new EffectSetting {Rainbow=zone.AudioMode==AudioZoneMode.SolidRainbow});
        }
        finally {_loadingZoneParameters=false;}
        NotifyZoneParameters();
    }
    private void RemoveRow(ZoneRowVM row)
    {
        int index = ZoneRows.IndexOf(row);
        if (index < 0) return;
        if (ReferenceEquals(_editing, row)) SelectNone();
        ZoneRows.RemoveAt(index);
        _zones.RemoveAt(index);
        Push();
    }
    public Action? ZonesChanged {get;set;}
    private void Push()
    {
        _controller.SetZones(_zones.ToArray());
        _settings.Settings.Zones = _zones.Select(ZoneMapping.ToSetting).ToList();
        _settings.Save();
        ZonesChanged?.Invoke();RefreshPreview();
    }
    public void RefreshPreview()
    {
        var frame = _controller.LastSentFrame;
        long now = Stopwatch.GetTimestamp(), frames = _controller.FramesRendered;
        double elapsed = Stopwatch.GetElapsedTime(_sampleTime, now).TotalSeconds;
        if (frame.IsEmpty || elapsed >= 0.5)
        {
            ActualFps = frame.IsEmpty ? 0 : Math.Max(0, frames - _sampleFrames) / elapsed;
            _sampleTime = now;
            _sampleFrames = frames;
        }
        ApplyPreviewFrame(frame);
    }

    public void ApplyPreviewFrame(ReadOnlyMemory<Rgb> frame)
    {
        bool hasDecorations = LightBarSegments.Count > 0 && frame.Length == Keys.Count + LightBarSegments.Count;
        if (frame.Length != Keys.Count && !hasDecorations) frame = ReadOnlyMemory<Rgb>.Empty;
        if (_previewFrame.Equals(frame)) return;
        _previewFrame = frame;
        HasLivePreview = !frame.IsEmpty;
        foreach (var key in Keys)
        {
            key.SetPreviewColor(HasLivePreview ? frame.Span[key.Index] : Rgb.Black);
        }
        for(int i=0;i<StripZoneSegments.Count;i++)
            StripZoneSegments[i].SetPreviewColor(!HasLivePreview?Rgb.Black:frame.Span[hasDecorations?83+i:Falchion75Layout.LightBarSourceIndices[i]]);
        for(int i=0;i<LightBarSegments.Count;i++)
        {
            var segment = LightBarSegments[i];
            segment.SetPreviewColor(!HasLivePreview ? Rgb.Black : frame.Span[hasDecorations ? Keys.Count+i : segment.Index]);
        }
    }
}
