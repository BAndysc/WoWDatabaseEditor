using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

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

// Content presenter, but will stretch TextBox children and right align otherwise
internal class SettingItemContentPresenter : ContentPresenter
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is TextBox || Child != null && Child.HorizontalAlignment == HorizontalAlignment.Stretch)
        {
            if (HorizontalContentAlignment != HorizontalAlignment.Stretch)
                HorizontalContentAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            if (HorizontalContentAlignment != HorizontalAlignment.Right)
                HorizontalContentAlignment = HorizontalAlignment.Right;
        }
        return base.ArrangeOverride(finalSize);
    }
}