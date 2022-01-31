using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace AvaloniaGraph.Controls;

public class ConnectionItem : ListBoxItem
{
    public static readonly StyledProperty<Point> TopLeftPositionProperty =
        AvaloniaProperty.Register<GraphNodeItemView, Point>(nameof(TopLeftPosition),
            defaultBindingMode: BindingMode.TwoWay);

    private GraphControl? ParentGraphControl => this.FindAncestorOfType<GraphControl>();

    public Point TopLeftPosition
    {
        get => GetValue(TopLeftPositionProperty);
        set => SetValue(TopLeftPositionProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        DoSelection();

        e.Handled = true;

        base.OnPointerPressed(e);
    }

    private void DoSelection()
    {
        if (ParentGraphControl == null)
            return;

        ParentGraphControl.ClearSelection();
        IsSelected = true;
    }
}