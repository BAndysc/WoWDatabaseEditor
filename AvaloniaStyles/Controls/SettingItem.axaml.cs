using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaloniaStyles.Controls;

public class SettingItem : ContentControl
{
    private string? help;
    public static readonly DirectProperty<SettingItem, string?> HelpProperty = AvaloniaProperty.RegisterDirect<SettingItem, string?>("Help", o => o.Help, (o, v) => o.Help = v);
    private string header = "Header";
    public static readonly DirectProperty<SettingItem, string> HeaderProperty = AvaloniaProperty.RegisterDirect<SettingItem, string>("Header", o => o.Header, (o, v) => o.Header = v);

    public string? Help
    {
        get => help;
        set => SetAndRaise(HelpProperty, ref help, value);
    }

    public string Header
    {
        get => header;
        set => SetAndRaise(HeaderProperty, ref header, value);
    }
}