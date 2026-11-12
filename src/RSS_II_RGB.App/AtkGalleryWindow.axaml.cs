using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace RSS_II_RGB.App;

public partial class AtkGalleryWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private AtkGalleryViewModel? Model => DataContext as AtkGalleryViewModel;
    public AtkGalleryWindow()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => Model?.RenderPreview();
        Opened += async (_, _) => { _timer.Start(); if (Model is { } model) await model.LoadAsync(); };
        Closed += (_, _) => { _timer.Stop(); Model?.Dispose(); };
    }
    private async void OnSearch(object? sender, RoutedEventArgs e) { if (Model is { } model) await model.LoadAsync(); }
    private async void OnSearchKey(object? sender, KeyEventArgs e) { if (e.Key == Key.Enter && Model is { } model) { e.Handled = true; await model.LoadAsync(); } }
    private async void OnSortChanged(object? sender, SelectionChangedEventArgs e) { if (IsVisible && Model is { } model) await model.LoadAsync(); }
    private async void OnApply(object? sender, RoutedEventArgs e) { if (Model is { } model && await model.ApplyAsync()) Close(); }
}
