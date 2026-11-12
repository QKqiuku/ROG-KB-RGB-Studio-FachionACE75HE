using RSS_II_RGB.App;
using Avalonia.Controls;
using Avalonia.Input;
namespace ROG_KB_RGB_Studio.App.Tests;
public class WorkbenchInputTests
{
    [Fact]
    public void OrdinaryKeysCannotActivateLightingControls()
    {
        foreach(var control in new Control[]{new CheckBox(),new Button(),new Slider(),new ComboBox()})
            foreach(var key in new[]{Key.Space,Key.Enter,Key.Left,Key.Right,Key.Up,Key.Down,Key.Home,Key.End,Key.A})
            {
                var e=new KeyEventArgs {Source=control,Key=key};WorkbenchInput.SuppressLightingActivation(null,e);
                Assert.True(e.Handled);
            }
    }
    [Fact]
    public void TextEditingIncludingSpacesIsStillAvailable()
    {
        foreach(var key in new[]{Key.Space,Key.Enter,Key.Left,Key.Right,Key.A,Key.Back})
        {
            var e=new KeyEventArgs {Source=new TextBox(),Key=key};WorkbenchInput.SuppressLightingActivation(null,e);
            Assert.False(e.Handled);
        }
    }
    [Fact]
    public void TabEscapeAndWindowCloseRemainAvailable()
    {
        foreach(var key in new[]{Key.Tab,Key.Escape,Key.F4})
        {
            var e=new KeyEventArgs {Source=new CheckBox(),Key=key,KeyModifiers=key==Key.F4?KeyModifiers.Alt:KeyModifiers.None};
            WorkbenchInput.SuppressLightingActivation(null,e);Assert.False(e.Handled);
        }
    }
}
