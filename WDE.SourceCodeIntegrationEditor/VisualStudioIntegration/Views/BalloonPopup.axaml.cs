using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.Views;

public class BalloonPopup : ContentControl
{
    public static readonly StyledProperty<bool> ShowTailProperty = AvaloniaProperty.Register<BalloonPopup, bool>(nameof(ShowTail));

    public bool ShowTail
    {
        get => GetValue(ShowTailProperty);
        set => SetValue(ShowTailProperty, value);
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