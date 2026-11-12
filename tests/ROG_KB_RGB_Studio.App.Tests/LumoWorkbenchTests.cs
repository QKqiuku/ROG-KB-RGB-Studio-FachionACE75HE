using RSS_II_RGB.App;
using Avalonia.Media;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Effects.Layers;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;

namespace ROG_KB_RGB_Studio.App.Tests;

public class LumoWorkbenchTests
{
    [Theory]
    [InlineData((int)EffectChoice.KeyHeart)]
    [InlineData((int)EffectChoice.KeyDoubleRing)]
    public async Task RetiredKeyEffectCannotBeSelectedOrRestored(int value)
    {
        var effect=(EffectChoice)value;
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);store.Settings.ReactiveEffect=effect;store.Save();
            var vm=new MainViewModel(controller,store,new Startup());vm.RestoreSettings();
            Assert.DoesNotContain(effect,vm.ReactiveChoices);
            Assert.Null(PresetCatalog.KeyPatternFor(effect));
            Assert.Equal(EffectChoice.Reactive,vm.SelectedReactiveEffect);
            vm.SelectedReactiveEffect=effect;
            Assert.Equal(EffectChoice.Reactive,vm.SelectedReactiveEffect);
        }
        finally {File.Delete(path);}
    }
    [Fact]
    public async Task InteractionSelectionPreservesBaseAndParametersSurviveRestart()
    {
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var settings=new SettingsService(path);settings.Settings.TargetFps=45;
            settings.Settings.Zones=[new ZoneSetting {KeyIndices=[46],ColorHex="FF0000"}];
            var vm=new MainViewModel(controller,settings,new Startup());vm.RestoreSettings();
            vm.RegularPresets.Single(p=>p.Effect==EffectChoice.Marquee).SelectCommand.Execute(null);
            vm.ReactivePresets.Single(p=>p.Effect==EffectChoice.KeyDiamond).SelectCommand.Execute(null);
            Assert.True(vm.IsCustomMode);Assert.True(vm.EnableReactive);
            Assert.Equal(EffectChoice.Marquee,vm.SelectedEffect);
            vm.ReactiveSpeed=1.8;vm.ReactiveWidth=1.3;vm.ReactiveBrightness=67;vm.ReactiveColor=Colors.Magenta;
            Assert.False(vm.ReactivePalette);
            Assert.Equal(KeyEffectKind.Diamond,Assert.Single(controller.BuildLayers().OfType<ReactivePatternLayer>()).Kind);
            Assert.Single(controller.BuildLayers().OfType<PatternLayer>(),l=>l.Id=="global");
            var restored=new MainViewModel(controller,new SettingsService(path),new Startup());restored.RestoreSettings();
            Assert.Equal(EffectChoice.KeyDiamond,restored.SelectedReactiveEffect);
            Assert.Equal(1.8,restored.ReactiveSpeed);Assert.Equal(1.3,restored.ReactiveWidth);Assert.Equal(67,restored.ReactiveBrightness);
            Assert.Equal(Colors.Magenta,restored.ReactiveColor);Assert.Equal(45,restored.TargetFps);Assert.Empty(restored.Editor!.ZoneRows);
            restored.RegularPresets.Single(p=>p.Effect==EffectChoice.Sun).SelectCommand.Execute(null);
            Assert.False(restored.IsCustomMode);Assert.Empty(controller.BuildLayers().OfType<ReactivePatternLayer>());
            restored.ChooseCustomCommand.Execute(null);
            Assert.False(restored.EnableReactive);Assert.Empty(controller.BuildLayers().OfType<ReactivePatternLayer>());
            Assert.Equal(EffectChoice.Sun,restored.SelectedEffect);
        }
        finally{File.Delete(path);}
    }
    [Fact]
    public async Task ReactiveStateIsKeptAcrossUnrelatedRebuilds()
    {
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        controller.EnableCustomLayers=true;controller.EnableReactive=true;controller.ReactiveEffect=EffectChoice.KeyDye;
        var first=Assert.Single(controller.BuildLayers().OfType<ReactivePatternLayer>());
        controller.AudioSensitivity=1.2;
        Assert.Same(first,Assert.Single(controller.BuildLayers().OfType<ReactivePatternLayer>()));
        controller.ReactiveEffect=EffectChoice.Reactive;
        Assert.Empty(controller.BuildLayers().OfType<ReactivePatternLayer>());
        controller.ReactiveEffect=EffectChoice.KeyDye;
        Assert.NotSame(first,Assert.Single(controller.BuildLayers().OfType<ReactivePatternLayer>()));
        controller.ReactiveEffect=EffectChoice.KeyDiamond;
        Assert.NotSame(first,Assert.Single(controller.BuildLayers().OfType<ReactivePatternLayer>()));
        controller.ReactiveBrightness=0;
        Assert.Empty(controller.BuildLayers().OfType<ReactivePatternLayer>());
    }
    [Fact]
    public async Task AllRequestedChoicesAreReachableAndKeyEffectsAreNotAmbientAnimations()
    {
        Assert.Equal(7,PresetCatalog.KeyEffects.Length);
        Assert.All(PresetCatalog.KeyEffects,e=>{Assert.NotNull(PresetCatalog.KeyPatternFor(e));Assert.DoesNotContain(e,PresetCatalog.Animations);});
        Assert.DoesNotContain(EffectChoice.Monet,PresetCatalog.All);
        Assert.Contains(EffectChoice.SineWave,PresetCatalog.Regular);
        Assert.Contains(EffectChoice.SineSectors,PresetCatalog.Regular);
        Assert.DoesNotContain(EffectChoice.MulticolorSineSectors,PresetCatalog.Regular);
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        controller.SetGlobalEffect(EffectChoice.Monet,new(255,255,255),1);
        Assert.Empty(controller.BuildLayers().OfType<ScreenPaletteLayer>());
    }
    [Fact]
    public void RequestedPresetCleanupMovesAmbientEffectsAndRippleToTheirProperSections()
    {
        foreach(var removed in new[]{EffectChoice.Monet,EffectChoice.Rain,EffectChoice.Waterfall,EffectChoice.Bloom})
        {
            Assert.DoesNotContain(removed,PresetCatalog.All);
            Assert.True(PresetCatalog.IsRemoved(removed));
        }
        Assert.DoesNotContain(EffectChoice.Ripple,PresetCatalog.Regular);
        Assert.Contains(EffectChoice.Ripple,PresetCatalog.KeyEffects);
        Assert.NotNull(PresetCatalog.KeyPatternFor(EffectChoice.Ripple));
        Assert.Equal(new[]{EffectChoice.BrickBreaker,EffectChoice.CountdownFireworks},PresetCatalog.Animations);
        Assert.Contains(EffectChoice.SineSectors,PresetCatalog.Regular);
        Assert.Contains(EffectChoice.Fire,PresetCatalog.Regular);
    }

    [Fact]
    public async Task SavedAmbientRippleBecomesAKeyEffectWithoutLosingItsSettings()
    {
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);
            store.Settings.GlobalEffect=EffectChoice.Ripple;store.Settings.UseCustomLayers=false;
            store.Settings.EffectSettings[EffectChoice.Ripple]=new EffectSetting{Speed=1.8,Width=1.4,ColorHex="FF5500",Rainbow=false};
            store.Save();
            var vm=new MainViewModel(controller,store,new Startup());vm.RestoreSettings();
            Assert.Equal(EffectChoice.Off,vm.SelectedEffect);
            Assert.Equal(EffectChoice.Ripple,vm.SelectedReactiveEffect);
            Assert.True(vm.IsCustomMode);Assert.True(vm.EnableReactive);
            Assert.Equal(1.8,vm.ReactiveSpeed);Assert.Equal(1.4,vm.ReactiveWidth);
            Assert.Equal(Color.FromRgb(255,85,0),vm.ReactiveColor);
        }
        finally{File.Delete(path);}
    }

    [Fact]
    public async Task InactiveSavedRippleBaseDoesNotReplaceTheCurrentPreset()
    {
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);
            store.Settings.GlobalEffect=EffectChoice.Solid;store.Settings.UseCustomLayers=false;
            store.Settings.CustomBaseEffect=EffectChoice.Ripple;
            store.Settings.EffectSettings[EffectChoice.Ripple]=new EffectSetting{Speed=1.8,ColorHex="FF5500",Rainbow=false};
            store.Save();
            var vm=new MainViewModel(controller,store,new Startup());vm.RestoreSettings();
            Assert.Equal(EffectChoice.Solid,vm.SelectedEffect);Assert.False(vm.IsCustomMode);
            vm.ChooseCustomCommand.Execute(null);
            Assert.Equal(EffectChoice.Solid,vm.SelectedEffect);
            Assert.False(vm.EnableReactive);Assert.Equal(EffectChoice.Ripple,vm.SelectedReactiveEffect);
            Assert.Equal(1.8,vm.ReactiveSpeed);Assert.Equal(Color.FromRgb(255,85,0),vm.ReactiveColor);
        }
        finally{File.Delete(path);}
    }

    [Fact]
    public async Task KeyRainbowIsExclusiveWithPaletteAndPersistsPerEffect()
    {
        string path=Path.GetTempFileName();
        await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var vm=new MainViewModel(controller,new SettingsService(path),new Startup());vm.RestoreSettings();
            vm.ReactivePresets.Single(p=>p.Effect==EffectChoice.Ripple).SelectCommand.Execute(null);
            vm.ReactiveRainbow=true;
            Assert.False(vm.ReactivePalette);Assert.False(vm.ReactiveUsesCustomColor);
            Assert.True(controller.ReactiveOptions.ReactiveRainbow);Assert.True(controller.ReactiveOptions.Rainbow);
            var restored=new MainViewModel(controller,new SettingsService(path),new Startup());restored.RestoreSettings();
            Assert.True(restored.ReactiveRainbow);Assert.False(restored.ReactivePalette);
            restored.ReactivePalette=true;
            Assert.False(restored.ReactiveRainbow);Assert.True(restored.ReactivePalette);
            restored.ReactiveColor=Colors.Red;
            Assert.False(restored.ReactivePalette);Assert.False(restored.ReactiveRainbow);Assert.True(restored.ReactiveUsesCustomColor);
        }
        finally{File.Delete(path);}
    }

    private sealed class QuietLog:ILogSink {public void Log(LogLevel level,string message,Exception? exception=null){}}
    private sealed class Startup:IStartupManager {public bool IsSupported=>false;public bool IsEnabled()=>false;public void SetEnabled(bool enabled){}}
}
