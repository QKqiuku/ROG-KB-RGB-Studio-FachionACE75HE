using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;
using RSS_II_RGB.Core.Rendering;

namespace RSS_II_RGB.App;

/// <summary>One selectable key in the visual keyboard, positioned by its grid cell.</summary>
internal sealed partial class KeyVM : ObservableObject
{
    public int Index { get; }
    public string Name { get; }
    public string ShortLabel { get; }
    public bool IsRogEye => Name == AppConstants.RogEyePreviewKeyName;
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double LabelSize => ShortLabel.Length > 5 ? 9 : 11;

    [ObservableProperty]
    private IBrush _previewBrush = Brushes.Black;

    [ObservableProperty]
    private IBrush _previewForeground = Brushes.White;

    private Rgb _previewColor;

    public void SetPreviewColor(Rgb color)
    {
        if (_previewColor == color) return;
        _previewColor = color;
        PreviewBrush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        PreviewForeground = color.R * 299 + color.G * 587 + color.B * 114 >= 150_000
            ? Brushes.Black : Brushes.White;
    }

    [ObservableProperty]
    private bool _isSelected;

    public KeyVM(int index, string name, double x, double y, double width = 35, double height = 35, string? label = null)
    {
        Index = index;
        Name = name;
        ShortLabel = label ?? MakeLabel(name);
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    private static string MakeLabel(string name) => name switch
    {
        "ESCAPE" => "Esc",
        "BACK_TICK" => "`",
        "TAB" => "Tab",
        "CAPS_LOCK" => "Caps",
        "LEFT_SHIFT" or "RIGHT_SHIFT" => "Shift",
        "LEFT_CONTROL" or "RIGHT_CONTROL" => "Ctrl",
        "LEFT_ALT" or "RIGHT_ALT" => "Alt",
        "LEFT_WINDOWS" => "Win",
        "MENU" => "Mn",
        "RIGHT_FUNCTION" => "Fn",
        "SPACE" => "Space",
        "BACKSPACE" => "Backspace",
        "ANSI_ENTER" or "NUMPAD_ENTER" => "Enter",
        "ANSI_BACK_SLASH" => "\\",
        "FORWARD_SLASH" => "/",
        "SEMICOLON" => ";",
        "QUOTE" => "'",
        "COMMA" => ",",
        "PERIOD" => ".",
        "MINUS" => "-",
        "EQUALS" => "=",
        "LEFT_BRACKET" => "[",
        "RIGHT_BRACKET" => "]",
        "PRINT_SCREEN" => "Prt",
        "SCROLL_LOCK" => "Scr",
        "PAUSE_BREAK" => "Pau",
        "INSERT" => "Ins",
        "DELETE" => "Del",
        "HOME" => "Home",
        "END" => "End",
        "PAGE_UP" => "PgUp",
        "PAGE_DOWN" => "PgDn",
        "UP_ARROW" => "↑",
        "DOWN_ARROW" => "↓",
        "LEFT_ARROW" => "←",
        "RIGHT_ARROW" => "→",
        "NUMPAD_LOCK" => "Num",
        "NUMPAD_DIVIDE" => "/",
        "NUMPAD_TIMES" => "*",
        "NUMPAD_MINUS" => "-",
        "NUMPAD_PLUS" => "+",
        "NUMPAD_PERIOD" => ".",
        "Logo" => "◆",
        _ => name.StartsWith("NUMPAD_", StringComparison.Ordinal) ? name["NUMPAD_".Length..] : name,
    };
}
