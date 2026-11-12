using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
namespace RSS_II_RGB.App;

/// <summary>Typing edits text; ambient key presses never activate lighting controls.</summary>
internal static class WorkbenchInput
{
    public static void Attach(InputElement scope)
    {
        scope.AddHandler(InputElement.KeyDownEvent,SuppressLightingActivation,RoutingStrategies.Tunnel);
        scope.AddHandler(InputElement.KeyUpEvent,SuppressLightingActivation,RoutingStrategies.Tunnel);
    }
    internal static void SuppressLightingActivation(object? sender,KeyEventArgs e)
    {
        if(e.Key is Key.Tab or Key.Escape || e.Key==Key.F4 && e.KeyModifiers.HasFlag(KeyModifiers.Alt))return;
        if(e.Source is Visual visual && (visual is TextBox || visual.GetVisualAncestors().Any(v=>v is TextBox)))return;
        e.Handled=true;
    }
}
