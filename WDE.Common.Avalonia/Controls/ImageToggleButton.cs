using Avalonia;
using Avalonia.Controls.Primitives;

namespace WDE.Common.Avalonia.Controls;

public class ImageToggleButton : ToggleButton
{
    public static readonly StyledProperty<string?> ImageCheckedProperty = AvaloniaProperty.Register<ImageToggleButton, string?>("ImageChecked");
    public static readonly StyledProperty<string?> ImageUncheckedProperty = AvaloniaProperty.Register<ImageToggleButton, string?>("ImageUnchecked");

    public string? ImageChecked
    {
        get => (string?)GetValue(ImageCheckedProperty);
        set => SetValue(ImageCheckedProperty, value);
    }

    public string? ImageUnchecked
    {
        get => (string?)GetValue(ImageUncheckedProperty);
        set => SetValue(ImageUncheckedProperty, value);
    }
}