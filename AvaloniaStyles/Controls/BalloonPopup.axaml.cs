using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AvaloniaStyles.Controls;

public class BalloonPopup : ContentControl
{
    public static readonly StyledProperty<bool> ShowTailProperty = AvaloniaProperty.Register<BalloonPopup, bool>(nameof(ShowTail));
    public static readonly StyledProperty<HorizontalAlignment> TailAlignmentProperty = AvaloniaProperty.Register<BalloonPopup, HorizontalAlignment>(nameof(TailAlignment));

    public bool ShowTail
    {
        get => GetValue(ShowTailProperty);
        set => SetValue(ShowTailProperty, value);
    }

    public HorizontalAlignment TailAlignment
    {
        get => GetValue(TailAlignmentProperty);
        set => SetValue(TailAlignmentProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShowTailProperty)
        {
            PseudoClasses.Set(":showtail", ShowTail);
        }
    }
}