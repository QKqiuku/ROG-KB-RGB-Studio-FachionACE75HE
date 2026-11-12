using System.Text.Json;
using RSS_II_RGB.Core.Audio;
using RSS_II_RGB.Core.Engine;
namespace RSS_II_RGB.App;

/// <summary>Only lighting state; library, audio target, FPS and app preferences stay outside the snapshot.</summary>
internal sealed class LightingComposition
{
    public EffectChoice BaseEffect {get;set;}
    public EffectSetting Base {get;set;}=new();
    public bool EnableReactive {get;set;}
    public EffectChoice ReactiveEffect {get;set;}=EffectChoice.Reactive;
    public EffectSetting Reactive {get;set;}=new();
    public bool EnableAudio {get;set;}
    public bool EnableStarlight {get;set;}
    public AudioAnalysisOptions MusicAudioOptions {get;set;}=new();
    public double AudioSensitivity {get;set;}=.9;
    public AudioLayoutChoice AudioLayout {get;set;}
    public double AudioBarsBassMultiplier {get;set;}=.9;
    public double AudioBarsMidMultiplier {get;set;}=1;
    public double AudioBarsTrebleMultiplier {get;set;}=2;
    public bool ShowMetrics {get;set;}
    public MetricLayoutChoice MetricLayout {get;set;}
    public double[] PercentThresholds {get;set;}=[30,60,90];
    public double[] TempThresholds {get;set;}=[55,65,75];
    public DecorationMode LightBarMode {get;set;}
    public DecorationMode EyeMode {get;set;}
    public OutputEnvelopeOptions OverallEffect {get;set;}=new();
    public DecorationSetting BarEffect {get;set;}=new();
    public DecorationSetting EyeEffect {get;set;}=new();
    public List<ZoneSetting> Zones {get;set;}=[];
    public string? GifAnimPath {get;set;}
    public string? GifSourcePath {get;set;}
    public int[] GifCrop {get;set;}=[];
    public string? AtkAnimPath {get;set;}
    public string? AtkPresetName {get;set;}
    public string? AtkPresetAuthor {get;set;}
    public string? AtkPresetId {get;set;}
    public bool AtkFitLayout {get;set;}

    public async Task PreserveAssetsAsync(string directory)
    {
        if(BaseEffect==EffectChoice.Gif && !string.IsNullOrEmpty(GifAnimPath))GifAnimPath=await CacheAssetAsync(GifAnimPath,directory,".kbanim");
        if(BaseEffect==EffectChoice.Atk && !string.IsNullOrEmpty(AtkAnimPath))AtkAnimPath=await CacheAssetAsync(AtkAnimPath,directory,".json");
    }
    private static async Task<string> CacheAssetAsync(string source,string directory,string extension)
    {
        await using var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.Asynchronous);
        string hash=Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(input));
        Directory.CreateDirectory(directory);string destination=Path.Combine(directory,hash+extension);
        if(File.Exists(destination))return destination;
        string temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            input.Position=0;
            await using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,FileOptions.Asynchronous))await input.CopyToAsync(output);
            File.Move(temporary,destination,true);
        }
        finally {try {File.Delete(temporary);} catch {}}
        // shortcut: immutable asset blobs are retained; add reference-based cleanup if storage grows.
        return destination;
    }

    private LightingComposition Copy()=>JsonSerializer.Deserialize(JsonSerializer.Serialize(this,AppSettingsJsonContext.Default.LightingComposition),AppSettingsJsonContext.Default.LightingComposition)!;
    public static LightingComposition Capture(AppSettings s)=>new LightingComposition
    {
        BaseEffect=s.GlobalEffect,Base=s.EffectSettings.GetValueOrDefault(s.GlobalEffect)??PresetCatalog.DefaultSettings(s.GlobalEffect),
        EnableReactive=s.EnableReactive,ReactiveEffect=s.ReactiveEffect,Reactive=s.EffectSettings.GetValueOrDefault(s.ReactiveEffect)??PresetCatalog.DefaultReactiveSettings(),
        EnableAudio=s.EnableAudio,EnableStarlight=s.EnableStarlight,MusicAudioOptions=s.MusicAudioOptions,
        AudioSensitivity=s.AudioSensitivity,AudioLayout=s.AudioLayout,AudioBarsBassMultiplier=s.AudioBarsBassMultiplier,AudioBarsMidMultiplier=s.AudioBarsMidMultiplier,AudioBarsTrebleMultiplier=s.AudioBarsTrebleMultiplier,
        ShowMetrics=s.ShowMetrics,MetricLayout=s.MetricLayout,PercentThresholds=s.PercentThresholds,TempThresholds=s.TempThresholds,
        LightBarMode=s.LightBarMode,EyeMode=s.EyeMode,OverallEffect=s.OverallEffect,BarEffect=s.BarEffect,EyeEffect=s.EyeEffect,Zones=s.Zones,
        GifAnimPath=s.GifAnimPath,GifSourcePath=s.GifSourcePath,GifCrop=s.GifCrop,AtkAnimPath=s.AtkAnimPath,AtkPresetName=s.AtkPresetName,AtkPresetAuthor=s.AtkPresetAuthor,AtkPresetId=s.AtkPresetId,AtkFitLayout=s.AtkFitLayout
    }.Copy();

    public void ApplyTo(AppSettings s,int ledCount)
    {
        var c=Copy();
        s.UseCustomLayers=true;s.GlobalEffect=Enum.IsDefined(c.BaseEffect)?PresetCatalog.Available(c.BaseEffect):EffectChoice.Rainbow;s.CustomBaseEffect=s.GlobalEffect;
        s.EffectSettings[s.GlobalEffect]=c.Base??PresetCatalog.DefaultSettings(s.GlobalEffect);
        if(c.BaseEffect==EffectChoice.MulticolorSineSectors && c.Base is null)s.EffectSettings[s.GlobalEffect].Rainbow=true;
        s.GlobalColorHex=s.EffectSettings[s.GlobalEffect].ColorHex;s.BrightnessPercent=s.EffectSettings[s.GlobalEffect].Brightness;
        s.EnableReactive=c.EnableReactive;s.ReactiveEffect=PresetCatalog.KeyChoices.Contains(c.ReactiveEffect)?c.ReactiveEffect:EffectChoice.Reactive;
        s.EffectSettings[s.ReactiveEffect]=c.Reactive??PresetCatalog.DefaultReactiveSettings();
        s.EnableAudio=c.EnableAudio;s.EnableStarlight=c.EnableStarlight;s.MusicAudioOptions=c.MusicAudioOptions??new();
        s.AudioSensitivity=c.AudioSensitivity;s.AudioLayout=c.AudioLayout;s.AudioBarsBassMultiplier=c.AudioBarsBassMultiplier;s.AudioBarsMidMultiplier=c.AudioBarsMidMultiplier;s.AudioBarsTrebleMultiplier=c.AudioBarsTrebleMultiplier;
        s.ShowMetrics=c.ShowMetrics;s.MetricLayout=c.MetricLayout;
        s.PercentThresholds=Thresholds(c.PercentThresholds,100,[30,60,90]);s.TempThresholds=Thresholds(c.TempThresholds,120,[55,65,75]);
        s.LightBarMode=c.LightBarMode;s.EyeMode=c.EyeMode;s.OverallEffect=c.OverallEffect??new();s.BarEffect=c.BarEffect??new();s.EyeEffect=c.EyeEffect??new();
        s.Zones=(c.Zones??[]).Where(z=>z is not null).Select(z=>new ZoneSetting {KeyIndices=(z.KeyIndices??[]).Where(i=>i>=0 && i<ledCount).Distinct().ToArray(),Effect=Enum.IsDefined(z.Effect)?z.Effect:EffectChoice.Solid,ColorHex=z.ColorHex,AudioMode=z.AudioMode,Parameters=z.Parameters}).ToList();
        foreach(var zone in s.Zones)PresetCatalog.NormalizeZone(zone);
        s.GifAnimPath=c.GifAnimPath;s.GifSourcePath=c.GifSourcePath;s.GifCrop=c.GifCrop??[];
        s.AtkAnimPath=c.AtkAnimPath;s.AtkPresetName=c.AtkPresetName;s.AtkPresetAuthor=c.AtkPresetAuthor;s.AtkPresetId=c.AtkPresetId;s.AtkFitLayout=c.AtkFitLayout;
    }
    private static double[] Thresholds(double[]? values,double max,double[] fallback)=>values is {Length:>=3}?values.Take(3).Select((v,i)=>double.IsFinite(v)?Math.Clamp(v,0,max):fallback[i]).ToArray():fallback;
}
internal sealed record UserLightingPreset
{
    public string Id {get;init;}="";
    public string Name {get;init;}="";
    public string KeyboardProfile {get;init;}="";
    public int KeyCount {get;init;}
    public LightingComposition Composition {get;init;}=new();
}
