using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RSS_II_RGB.Core.Layout;
namespace RSS_II_RGB.App;

internal sealed partial class UserPresetItem : ObservableObject
{
    public string Id {get;}
    public string Name {get;}
    public IRelayCommand LoadCommand {get;}
    public IAsyncRelayCommand DeleteCommand {get;}
    [ObservableProperty] private bool _isSelected;
    public UserPresetItem(UserLightingPreset preset,Action<string> load,Func<string,Task> delete)
    {Id=preset.Id;Name=preset.Name;LoadCommand=new RelayCommand(()=>load(Id));DeleteCommand=new AsyncRelayCommand(()=>delete(Id));}
}
internal sealed partial class MainViewModel
{
    public ObservableCollection<UserPresetItem> UserPresets {get;}=[];
    private string? _activeUserPresetId;
    private bool _loadingUserPreset;
    [ObservableProperty] private string _userPresetName="";
    [ObservableProperty] private string _userPresetStatus="";
    [ObservableProperty] private bool _isSavingUserPreset;
    public bool CanRestoreUserPreset=>_activeUserPresetId is not null;
    private bool MatchesKeyboard(UserLightingPreset? p)=>p is not null && p.KeyboardProfile==_controller.Profile.Name && p.KeyCount==_controller.Profile.LedCount;
    private static bool ValidPresetName(string? name)=>!string.IsNullOrWhiteSpace(name) && name.Trim().Length<=48 && !name.Any(char.IsControl);
    private void RefreshUserPresetLibrary()
    {
        var s=_settings.Settings;s.UserPresets??=[];
        UserPresets.Clear();
        foreach(var preset in s.UserPresets.Where(p=>p is not null && !string.IsNullOrWhiteSpace(p.Id) && ValidPresetName(p.Name) && p.Composition is not null && MatchesKeyboard(p)))
            UserPresets.Add(new UserPresetItem(preset,LoadUserPreset,DeleteUserPresetAsync));
        foreach(var item in UserPresets)item.IsSelected=IsCustomMode && item.Id==_activeUserPresetId;
    }
    private void RestoreUserPresetLibrary()
    {
        RefreshUserPresetLibrary();
        SetActiveUserPreset(UserPresets.FirstOrDefault(p=>p.Id==_settings.Settings.ActiveUserPresetId)?.Id);
    }
    private void SetActiveUserPreset(string? id)
    {
        _activeUserPresetId=id;_settings.Settings.ActiveUserPresetId=id;
        UserPresetName=UserPresets.FirstOrDefault(p=>p.Id==id)?.Name??"";
        OnPropertyChanged(nameof(CanRestoreUserPreset));RestoreUserPresetCommand.NotifyCanExecuteChanged();
        foreach(var item in UserPresets)item.IsSelected=IsCustomMode && item.Id==id;
    }
    private void ForgetUserPreset()
    {
        if(_loadingUserPreset)return;
        SetActiveUserPreset(null);UserPresetStatus="";
    }
    [RelayCommand] private Task SaveUserPresetAsync()=>SaveNamedPresetAsync(false);
    [RelayCommand] private Task SaveUserPresetAsAsync()=>SaveNamedPresetAsync(true);
    private async Task SaveNamedPresetAsync(bool asNew)
    {
        if(!IsCustomMode || IsSavingUserPreset)return;
        string name=(UserPresetName??"").Trim();
        if(!ValidPresetName(name)) {UserPresetStatus=L.UserPresetInvalidName;return;}
        var s=_settings.Settings;s.UserPresets??=[];
        var existing=asNew?null:s.UserPresets.FirstOrDefault(p=>MatchesKeyboard(p) && p.Id==_activeUserPresetId);
        if(s.UserPresets.Any(p=>MatchesKeyboard(p) && ValidPresetName(p.Name) && p.Id!=existing?.Id && string.Equals(p.Name.Trim(),name,StringComparison.OrdinalIgnoreCase)))
        {UserPresetStatus=L.UserPresetDuplicateName;return;}
        IsSavingUserPreset=true;
        try
        {
            Apply();
            var composition=LightingComposition.Capture(s);
            await composition.PreserveAssetsAsync(_settings.UserPresetAssetDirectory);
            var saved=new UserLightingPreset {Id=existing?.Id??Guid.NewGuid().ToString("N"),Name=name,KeyboardProfile=_controller.Profile.Name,KeyCount=_controller.Profile.LedCount,Composition=composition};
            int index=existing is null?-1:s.UserPresets.IndexOf(existing);
            string? previousActive=s.ActiveUserPresetId;
            if(index<0)s.UserPresets.Add(saved);else s.UserPresets[index]=saved;
            s.ActiveUserPresetId=saved.Id;
            if(!await _settings.SaveAsync())
            {
                if(index<0)s.UserPresets.Remove(saved);else s.UserPresets[index]=existing!;
                s.ActiveUserPresetId=previousActive;UserPresetStatus=L.UserPresetSaveFailed;return;
            }
            RefreshUserPresetLibrary();SetActiveUserPreset(saved.Id);UserPresetStatus=L.UserPresetSaved;
        }
        catch(Exception ex) when(ex is System.Text.Json.JsonException or IOException or UnauthorizedAccessException) {UserPresetStatus=L.UserPresetSaveFailed;}
        finally {IsSavingUserPreset=false;}
    }
    private async Task DeleteUserPresetAsync(string id)
    {
        if(IsSavingUserPreset)return;
        var s=_settings.Settings;
        int index=s.UserPresets.FindIndex(p=>MatchesKeyboard(p) && p.Id==id);
        if(index<0)return;
        var removed=s.UserPresets[index];string? previousActive=s.ActiveUserPresetId;
        IsSavingUserPreset=true;
        try
        {
            s.UserPresets.RemoveAt(index);
            if(s.ActiveUserPresetId==id)s.ActiveUserPresetId=null;
            if(!await _settings.SaveAsync())
            {
                s.UserPresets.Insert(index,removed);s.ActiveUserPresetId=previousActive;
                UserPresetStatus=L.UserPresetDeleteFailed;return;
            }
            RefreshUserPresetLibrary();
            if(_activeUserPresetId==id)SetActiveUserPreset(null);
            UserPresetStatus=L.UserPresetDeleted;
        }
        finally {IsSavingUserPreset=false;}
    }
    private void LoadUserPreset(string id)
    {
        if(IsSavingUserPreset)return;
        var preset=_settings.Settings.UserPresets.FirstOrDefault(p=>MatchesKeyboard(p) && p.Id==id);
        if(preset is null)return;
        _loadingUserPreset=true;
        try
        {
            int count=ReferenceEquals(_controller.Profile,Falchion75Layout.Profile)?Falchion75Layout.SpatialProfile.LedCount:_controller.Profile.LedCount;
            preset.Composition.ApplyTo(_settings.Settings,count);_settings.Settings.ActiveUserPresetId=id;
            _controller.SetGifAnimation(null,rebuild:false);_controller.SetAtkAnimation(null,rebuild:false);
            RestoreSettings(preserveAudioSource:true);
            Editor!.CanSelectKeys=IsCustomMode && IsConnected;
            UserPresetStatus=L.UserPresetLoaded;
        }
        catch(System.Text.Json.JsonException) {UserPresetStatus=L.UserPresetLoadFailed;}
        finally {_loadingUserPreset=false;}
    }
    [RelayCommand(CanExecute=nameof(CanRestoreUserPreset))]
    private void RestoreUserPreset() {if(_activeUserPresetId is { } id)LoadUserPreset(id);}
    [RelayCommand]
    private void ResetCurrentEffect()
    {
        _settings.Settings.EffectSettings[SelectedEffect]=PresetCatalog.DefaultSettings(SelectedEffect);
        LoadEffectSettings(SelectedEffect);Apply();
    }
    [RelayCommand]
    private void ResetReactiveEffect()
    {
        _settings.Settings.EffectSettings[SelectedReactiveEffect]=PresetCatalog.DefaultReactiveSettings();
        LoadReactiveSettings();Apply();
    }
    [RelayCommand]
    private void ResetOverallEffect()
    {
        _loadingEffect=true;OverallOpacity=100;OverallMinimum=10;OverallSpeed=1;_loadingEffect=false;Apply();
    }
}
