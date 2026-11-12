using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RSS_II_RGB.Core.Audio;

namespace RSS_II_RGB.App;

internal sealed record MusicSourceItem(AudioSource Source)
{
    public string Label => Source.ProcessId==0 ? L.MusicSystemAudio : Source.ProcessId<0 ? $"{Source.Name} · {L.MusicWaiting}" : $"{Source.Name} · {Source.ProcessId}";
}

internal sealed partial class MainViewModel
{
    private readonly IAudioControl? _audioControl;
    private readonly SemaphoreSlim _audioControlGate=new(1,1);
    private readonly CancellationTokenSource _audioLifetime=new();
    private DispatcherTimer? _audioTimer;
    private CancellationTokenSource? _audioDebounce;
    private bool _audioMonitoring, _restoringMusic, _sourceRestorePending;
    private int _audioPreviewConsumers, _audioPollTicks;
    private AudioControlRequest? _lastAudioRequest;
    public PresetItem[] MusicPresets { get; }
    internal MusicCanvasSettings? LiveMusicSettings=>PresetCatalog.MusicFor(SelectedEffect) is { } kind
        ? new(kind,new RSS_II_RGB.Core.Rendering.Rgb(PickedColor.R,PickedColor.G,PickedColor.B),_controller.BaseOptions,BrightnessPercent/100):null;
    internal RSS_II_RGB.Core.Audio.AudioFrame LiveMusicAudio=>_controller.AudioSnapshot;
    internal TimeSpan? LiveMusicTime=>_controller.MusicCanvasTime;
    public string MusicCanvasHintText=>SelectedEffect==EffectChoice.MusicSoundWave?L.MusicCanvasSoundWaveHint:L.MusicCanvasHint;
    public bool IsMusicEffect => PresetCatalog.MusicFor(SelectedEffect) is not null;
    public bool ShowMusicControls => IsMusicEffect || IsAtkEffect || IsCustomMode && _controller.NeedsAudio;
    public ObservableCollection<MusicSourceItem> MusicSources {get;} = [new(new AudioSource(0,"System"))];
    [ObservableProperty] private MusicSourceItem? _selectedMusicSource;
    [ObservableProperty] private string _musicCaptureStatus=L.MusicStopped;
    [ObservableProperty] private double _musicSignalLevel;
    [ObservableProperty] private double _musicSensitivity=1;
    [ObservableProperty] private double _musicAttack=.025;
    [ObservableProperty] private double _musicRelease=.18;
    [ObservableProperty] private double _musicNoiseGate=.002;
    [ObservableProperty] private bool _musicAutoGain=true;
    [ObservableProperty] private double _musicBassEmphasis=1;

    private void OnZoneEffectsChanged()
    {
        if(_loading || _loadingEffect)return;
        OnPropertyChanged(nameof(ShowMusicControls));QueueAudioConfiguration();
    }
    private void RestoreMusicSettings(bool preserveSource=false)
    {
        _restoringMusic=true;
        var s=_settings.Settings;
        var o=s.MusicAudioOptions??new();
        MusicSensitivity=Finite(o.Sensitivity,.1,4,1);MusicAttack=Finite(o.Attack,.005,.25,.025);
        MusicRelease=Finite(o.Release,.02,2,.18);MusicNoiseGate=Finite(o.NoiseGate,0,.05,.002);
        MusicAutoGain=o.AutoGain;MusicBassEmphasis=Finite(o.BassEmphasis,.25,3,1);
        if(preserveSource) {_restoringMusic=false;return;}
        _sourceRestorePending=!string.IsNullOrWhiteSpace(s.MusicSourcePath) || !string.IsNullOrWhiteSpace(s.MusicSourceName);
        if(_sourceRestorePending)
        {
            SelectedMusicSource=new(new AudioSource(-1,s.MusicSourceName??L.MusicSourceMissing,ExecutablePath:s.MusicSourcePath));
            MusicSources.Add(SelectedMusicSource);MusicCaptureStatus=L.MusicSourceMissing;
        }
        else SelectedMusicSource=MusicSources[0];
        _restoringMusic=false;
    }
    private AudioAnalysisOptions CurrentAudioOptions() => new()
    {
        Sensitivity=Finite(MusicSensitivity,.1,4,1),NoiseGate=Finite(MusicNoiseGate,0,.05,.002),
        Attack=Finite(MusicAttack,.005,.25,.025),Release=Finite(MusicRelease,.02,2,.18),
        AutoGain=MusicAutoGain,BassEmphasis=Finite(MusicBassEmphasis,.25,3,1)
    };
    private void SaveMusicSettings()
    {
        _settings.Settings.MusicAudioOptions=CurrentAudioOptions();
        if(_sourceRestorePending) return;
        var source=SelectedMusicSource?.Source;
        _settings.Settings.MusicSourcePath=source?.ProcessId>0 ? source.ExecutablePath:null;
        _settings.Settings.MusicSourceName=source?.ProcessId>0 ? source.Name:null;
    }
    internal void StartAudioMonitoring()
    {
        if(_audioControl is null || _audioMonitoring) return;
        _audioMonitoring=true;
        _audioTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(250)};
        _audioTimer.Tick+=OnAudioTimer;
        _audioTimer.Start();
        _=RefreshAudioSourcesAsync();
        QueueAudioConfiguration();
    }
    internal void StopAudioMonitoring()
    {
        _audioMonitoring=false;_audioTimer?.Stop();
        _audioDebounce?.Cancel();_audioDebounce?.Dispose();_audioLifetime.Cancel();
    }
    private async void OnAudioTimer(object? sender,EventArgs e)
    {
        var frame=_controller.AudioSnapshot;
        MusicSignalLevel=frame.IsFresh(Environment.TickCount64)?Math.Clamp(frame.Volume*100,0,100):0;
        if(++_audioPollTicks%8!=0) return;
        await ConfigureMusicAudioAsync();
        await PollAudioStatusAsync();
    }
    internal void SetAudioPreviewActive(bool active)
    {
        _audioPreviewConsumers=Math.Max(0,_audioPreviewConsumers+(active?1:-1));
        QueueAudioConfiguration();
    }
    private void QueueAudioConfiguration()
    {
        if(!_audioMonitoring || _audioControl is null) return;
        _audioDebounce?.Cancel();_audioDebounce?.Dispose();
        _audioDebounce=CancellationTokenSource.CreateLinkedTokenSource(_audioLifetime.Token);
        _=ApplyAudioAfterDelayAsync(_audioDebounce.Token);
    }
    private async Task ApplyAudioAfterDelayAsync(CancellationToken ct)
    {
        try {await Task.Delay(120,ct);await ConfigureMusicAudioAsync(ct);}
        catch(OperationCanceledException) { }
    }
    internal async Task ConfigureMusicAudioAsync(CancellationToken ct=default)
    {
        if(_audioControl is null || _audioLifetime.IsCancellationRequested) return;
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct,_audioLifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        bool entered=false;
        try
        {
            await _audioControlGate.WaitAsync(timeout.Token);entered=true;
            var source=SelectedMusicSource?.Source;
            var request=new AudioControlRequest {Command="configure",ProcessId=Math.Max(0,source?.ProcessId??0),
                StartTimeTicks=source?.StartTimeTicks??0,CaptureEnabled=source?.ProcessId>=0 && !_sourceRestorePending && (_controller.NeedsAudio || _audioPreviewConsumers>0),Options=CurrentAudioOptions()};
            if(request==_lastAudioRequest) return;
            var response=await _audioControl.SendAsync(request,timeout.Token);
            _lastAudioRequest=response.State is "error" or "unsupported"?null:request;
            UpdateAudioStatus(response);
        }
        catch(OperationCanceledException) {if(!ct.IsCancellationRequested && !_audioLifetime.IsCancellationRequested) MusicCaptureStatus=L.MusicUnavailable;}
        catch(Exception ex) {MusicCaptureStatus=L.MusicUnavailable+" · "+ex.Message;_lastAudioRequest=null;}
        finally {if(entered)_audioControlGate.Release();}
    }
    private async Task PollAudioStatusAsync()
    {
        if(_audioControl is null || _audioLifetime.IsCancellationRequested) return;
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(_audioLifetime.Token);timeout.CancelAfter(2500);
        try
        {
            var response=await _audioControl.SendAsync(new AudioControlRequest(),timeout.Token);
            UpdateAudioStatus(response);
            if(response.State=="stopped" && _lastAudioRequest?.CaptureEnabled==true) _lastAudioRequest=null;
            if(response.State=="waiting" && _lastAudioRequest?.CaptureEnabled==true && SelectedMusicSource?.Source.ProcessId>0)
            {
                await RefreshAudioSourcesAsync();
            }
        }
        catch(OperationCanceledException) { }
        catch {MusicCaptureStatus=L.MusicUnavailable;_lastAudioRequest=null;}
    }
    private void UpdateAudioStatus(AudioControlResponse response)
    {
        MusicCaptureStatus=response.State switch
        {
            "capturing"=>L.MusicCapturing,"stopped"=>L.MusicStopped,"unsupported"=>L.MusicProcessUnsupported,
            _=>L.MusicWaiting
        };
        if(!string.IsNullOrWhiteSpace(response.Error)) MusicCaptureStatus+=" · "+response.Error;
        if(_sourceRestorePending) MusicCaptureStatus=L.MusicSourceMissing;
    }
    [RelayCommand]
    internal async Task RefreshAudioSourcesAsync()
    {
        if(_audioControl is null) return;
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(_audioLifetime.Token);timeout.CancelAfter(3000);
        try
        {
            var response=await _audioControl.SendAsync(new AudioControlRequest {Command="list"},timeout.Token);
            _restoringMusic=true;
            var previous=SelectedMusicSource;
            MusicSources.Clear();MusicSources.Add(new(new AudioSource(0,"System")));
            foreach(var source in response.Sources.Where(s=>s.ProcessId>0).DistinctBy(s=>s.ProcessId)) MusicSources.Add(new(source));
            MusicSourceItem? selected=null;
            if(_sourceRestorePending)
            {
                var saved=_settings.Settings;
                var matches=MusicSources.Where(x=>x.Source.ProcessId>0 && (!string.IsNullOrEmpty(saved.MusicSourcePath)
                    ? string.Equals(x.Source.ExecutablePath,saved.MusicSourcePath,StringComparison.OrdinalIgnoreCase)
                    : string.Equals(x.Source.Name,saved.MusicSourceName,StringComparison.OrdinalIgnoreCase))).ToArray();
                if(matches.Length==1) {selected=matches[0];_sourceRestorePending=false;}
            }
            else
            {
                selected=MusicSources.FirstOrDefault(x=>x.Source.ProcessId==previous?.Source.ProcessId && x.Source.StartTimeTicks==previous.Source.StartTimeTicks);
                if(selected is null && previous?.Source.ProcessId>0)
                {
                    var matches=MusicSources.Where(x=>x.Source.ProcessId>0 && (!string.IsNullOrEmpty(previous.Source.ExecutablePath)
                        ? string.Equals(x.Source.ExecutablePath,previous.Source.ExecutablePath,StringComparison.OrdinalIgnoreCase)
                        : string.Equals(x.Source.Name,previous.Source.Name,StringComparison.OrdinalIgnoreCase))).ToArray();
                    if(matches.Length==1) selected=matches[0];
                    else _sourceRestorePending=true;
                }
            }
            if(selected is null && previous?.Source.ProcessId!=0 && previous is not null) {selected=previous;MusicSources.Add(previous);}
            SelectedMusicSource=selected??MusicSources[0];
            _restoringMusic=false;
            UpdateAudioStatus(response);QueueAudioConfiguration();
        }
        catch(OperationCanceledException) { }
        catch {MusicCaptureStatus=L.MusicUnavailable;}
        finally {_restoringMusic=false;}
    }
    partial void OnSelectedMusicSourceChanged(MusicSourceItem? value)
    {
        if(_restoringMusic || _loading) return;
        _sourceRestorePending=false;_lastAudioRequest=null;SaveMusicSettings();_settings.Save();QueueAudioConfiguration();
    }
    private void AudioOptionsChanged()
    {
        if(_restoringMusic || _loading || !_ready) return;
        SaveMusicSettings();_settings.Save();QueueAudioConfiguration();
    }
    partial void OnMusicSensitivityChanged(double value)=>AudioOptionsChanged();
    partial void OnMusicAttackChanged(double value)=>AudioOptionsChanged();
    partial void OnMusicReleaseChanged(double value)=>AudioOptionsChanged();
    partial void OnMusicNoiseGateChanged(double value)=>AudioOptionsChanged();
    partial void OnMusicAutoGainChanged(bool value)=>AudioOptionsChanged();
    partial void OnMusicBassEmphasisChanged(double value)=>AudioOptionsChanged();
}

internal sealed record MusicCanvasSettings(RSS_II_RGB.Core.Effects.Layers.MusicKind Kind,
    RSS_II_RGB.Core.Rendering.Rgb Color,RSS_II_RGB.Core.Effects.Layers.PatternOptions Options,double Brightness);
