using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RSS_II_RGB.App;

public partial class ZoneEditorView : UserControl
{
    public ZoneEditorView() => InitializeComponent();
    private void OnApplyZoneParameters(object? sender,RoutedEventArgs e)
    {
        if(DataContext is ZoneEditorViewModel vm && vm.AssignZoneCommand.CanExecute(null))
        {vm.AssignZoneCommand.Execute(null);RegionOptionsButton.Flyout?.Hide();}
    }
}
