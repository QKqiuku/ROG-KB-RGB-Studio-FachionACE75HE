using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using RSS_II_RGB.Core.Animation;
using RSS_II_RGB.Core.Layout;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.App;

internal sealed partial class AtkGalleryViewModel : ObservableObject, IDisposable
{
    private readonly MainViewModel _main;
    private readonly AtkGalleryClient _client;
    private readonly KeyboardProfile _profile;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Stopwatch _clock = new();
    private AtkScene? _preview;
    private int _generation;
    private readonly Rgb[] _pixels;
    public ObservableCollection<AtkPreset> Items { get; } = new();
    public KeyVM[] Keys { get; }
    public double CanvasWidth => KeyboardGeometry.Width(_main.Controller.Profile);
    public double CanvasHeight => KeyboardGeometry.Height(_main.Controller.Profile);
    public string LayoutFilterHint => string.Format(CultureInfo.InvariantCulture, L.AtkLayoutFilterFormat, L.FormFactorName(_profile.FormFactor), _profile.Name);
    public string[] SortOptions { get; } = [L.AtkHot, L.AtkLatest];
    public string[] MappingOptions { get; } = [L.AtkKeepGrid, L.AtkFit];
    [ObservableProperty] private string _keyword = "";
    [ObservableProperty] private int _sortIndex;
    [ObservableProperty] private int _mappingIndex;
    [ObservableProperty] private AtkPreset? _selectedItem;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private string _previewStatus = L.AtkSelect;
    [ObservableProperty] private bool _isPreparing;
    public bool CanApply => !IsBusy && !IsPreparing && _preview is not null && SelectedItem is {IsKeyframe:true}
        && ReferenceEquals(_profile, _main.Controller.Profile) && AtkLayoutFilter.Matches(SelectedItem, _profile);


    public AtkGalleryViewModel(MainViewModel main, AtkGalleryClient? client = null)
    {
        _main = main;
        _main.SetAudioPreviewActive(true);
        _client = client ?? new AtkGalleryClient();
        _profile = main.Controller.Profile;
        var profile = main.Controller.Profile;
        Keys = profile.Keys.ToArray().Select(k =>
        {
            var box = KeyboardGeometry.ForKey(profile, k);
            return new KeyVM(k.Index, k.Name, box.X, box.Y, box.Width, box.Height);
        }).ToArray();
        _pixels = new Rgb[profile.LedCount];
    }

    public async Task LoadAsync()
    {
        if (IsBusy || _lifetime.IsCancellationRequested) return;
        if (!ReferenceEquals(_profile, _main.Controller.Profile)) { Status = L.AtkKeyboardChanged; return; }
        IsBusy = true;
        Status = L.AtkLoading;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            deadline.CancelAfter(TimeSpan.FromMinutes(5));
            int scanned = 0, hidden = 0;
            long retainedCharacters = 0;
            var ids = new HashSet<string>();
            string keyword = Keyword;
            bool latest = SortIndex == 1;
            SelectedItem = null;
            Items.Clear();
            for (int remote = 1; ; remote++)
            {
                var result = await _client.GetPageAsync(keyword, latest, remote, deadline.Token);
                if (!ReferenceEquals(_profile, _main.Controller.Profile)) { Status = L.AtkKeyboardChanged; return; }
                scanned += result.ReturnedCount;
                hidden += result.ReturnedCount;
                foreach (var item in result.Items)
                {
                    if (!item.IsKeyframe || !AtkLayoutFilter.Matches(item, _profile) || !ids.Add(item.Id)) continue;
                    retainedCharacters += item.Scene.GetRawText().Length;
                    if (retainedCharacters > AppConstants.AtkMaxCatalogueCharacters)
                        throw new InvalidDataException("Gallery exceeds the retained scene budget.");
                    Items.Add(item);
                    hidden--;
                }
                Status = string.Format(CultureInfo.InvariantCulture, L.AtkCatalogueLoadingFormat, scanned, result.Total, Items.Count);
                if (result.ReturnedCount == 0 && (long)(remote - 1) * AppConstants.AtkPageSize < result.Total)
                    throw new InvalidDataException("Gallery ended before its declared total.");
                if ((long)remote * AppConstants.AtkPageSize >= result.Total) break;
            }
            if (_lifetime.IsCancellationRequested) return;
            Status = string.Format(CultureInfo.InvariantCulture, L.AtkCatalogueCompleteFormat, Items.Count, hidden);
            if (Items.Count == 0) Status += " Â· " + L.AtkNoMatchingLayout;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch { Status = string.Format(CultureInfo.InvariantCulture, L.AtkCatalogueIncompleteFormat, Items.Count); }
        finally { IsBusy = false; }
    }

    partial void OnSelectedItemChanged(AtkPreset? value) => _ = PreparePreviewAsync();
    partial void OnMappingIndexChanged(int value) => _ = PreparePreviewAsync();
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanApply));
    }
    partial void OnIsPreparingChanged(bool value) => OnPropertyChanged(nameof(CanApply));

    internal async Task PreparePreviewAsync()
    {
        int generation = ++_generation;
        _preview = null;
        Array.Clear(_pixels);
        foreach (var key in Keys) key.SetPreviewColor(Rgb.Black);
        var item = SelectedItem;
        bool fit = MappingIndex == 1;
        IsPreparing = true;
        PreviewStatus = item is null ? L.AtkSelect : L.AtkLoading;
        try
        {
            if (item is null) return;
            if (!item.IsKeyframe) { PreviewStatus = L.AtkGalleryHint; return; }
            if (!AtkLayoutFilter.Matches(item, _profile)) { PreviewStatus = L.AtkLayoutMismatch; return; }
            var scene = await Task.Run(() => AtkScene.Parse(item.Scene, _main.Controller.Profile, fit), _lifetime.Token);
            if (generation != _generation || _lifetime.IsCancellationRequested) return;
            _preview = scene;
            _clock.Restart();
            PreviewStatus = string.Format(CultureInfo.InvariantCulture, L.AtkFramesFormat, scene.FrameCount);
            RenderPreview();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (InvalidDataException error)
        {
            if (generation == _generation) PreviewStatus = L.AtkUnsupported + " ¡¤ " + error.Message[..Math.Min(error.Message.Length, 180)];
        }
        catch { if (generation == _generation) PreviewStatus = L.AtkUnsupported; }
        finally { if (generation == _generation) { IsPreparing = false; OnPropertyChanged(nameof(CanApply)); } }
    }

    public void RenderPreview()
    {
        if (!ReferenceEquals(_profile, _main.Controller.Profile))
        {
            _preview = null;
            PreviewStatus = L.AtkKeyboardChanged;
            OnPropertyChanged(nameof(CanApply));
            return;
        }
        if (_preview is null) return;
        _preview.Render(_pixels, _clock.Elapsed, _main.Controller.AudioSnapshot);
        foreach (var key in Keys) key.SetPreviewColor(_pixels[key.Index]);
    }

    public async Task<bool> ApplyAsync()
    {
        if (!CanApply) return false;
        IsBusy = true;
        try
        {
            await _main.ImportAtkAsync(SelectedItem!, _preview!, MappingIndex == 1);
            return true;
        }
        catch { PreviewStatus = L.AtkImportError; return false; }
        finally { IsBusy = false; }
    }

    public void Dispose() { _main.SetAudioPreviewActive(false); ++_generation; _lifetime.Cancel(); _lifetime.Dispose(); _clock.Stop(); }
}
