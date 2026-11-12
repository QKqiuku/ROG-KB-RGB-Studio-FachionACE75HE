using RSS_II_RGB.App;
using RSS_II_RGB.Core.Effects;
using RSS_II_RGB.Core.Engine;
using RSS_II_RGB.Core.Logging;
using RSS_II_RGB.Core.Rendering;
using RSS_II_RGB.Core.Sensors;
using RSS_II_RGB.Core.Startup;
namespace ROG_KB_RGB_Studio.App.Tests;
public class CustomEntryTests
{
    [Fact]
    public async Task EnteringCustomKeepsCurrentPresetAndDoesNotReviveAnOldComposition()
    {
        string path=Path.GetTempFileName();await using var controller=new KeyboardController(new QuietLog(),new SensorState());
        try
        {
            var store=new SettingsService(path);store.Settings.GlobalEffect=EffectChoice.Aurora;
            store.Settings.CustomBaseEffect=EffectChoice.Aurora;store.Settings.Zones=[new ZoneSetting {KeyIndices=[0],ColorHex="FF0000"}];store.Settings.OverallEffect=new() {Mode=OutputEnvelopeMode.Breathing};
            var vm=new MainViewModel(controller,store,new Startup());vm.RestoreSettings();
            vm.RegularPresets.Single(p=>p.Effect==EffectChoice.Wave).SelectCommand.Execute(null);vm.EffectSpeed=1.4;
            vm.ChooseCustomCommand.Execute(null);
            Assert.Equal(EffectChoice.Wave,vm.SelectedEffect);Assert.Equal(1.4,vm.EffectSpeed);Assert.Empty(controller.Zones);
            Assert.Equal(0,vm.OverallModeIndex);Assert.False(vm.EnableReactive);
            vm.LightBarModeIndex=2;vm.EyeModeIndex=2;
            vm.RegularPresets.Single(p=>p.Effect==EffectChoice.Solid).SelectCommand.Execute(null);
            Assert.Equal(1,vm.LightBarModeIndex);Assert.Equal(1,vm.EyeModeIndex);
            vm.LightBarModeIndex=2;vm.EyeModeIndex=2;Assert.Equal(1,vm.LightBarModeIndex);Assert.Equal(1,vm.EyeModeIndex);
            vm.ReactivePresets.Single(p=>p.Effect==EffectChoice.Ripple).SelectCommand.Execute(null);
            Assert.Equal(EffectChoice.Solid,vm.SelectedEffect);Assert.Empty(controller.Zones);Assert.True(vm.EnableReactive);
        }
        finally {File.Delete(path);}
    }
    private sealed class QuietLog:ILogSink {public void Log(LogLevel level,string message,Exception? exception=null){}}
    private sealed class Startup:IStartupManager {public bool IsEnabled()=>false;public void SetEnabled(bool enabled){}public bool IsSupported=>false;}
}
