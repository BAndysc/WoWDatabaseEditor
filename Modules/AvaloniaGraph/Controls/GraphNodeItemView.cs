using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaGraph.ViewModels;

namespace AvaloniaGraph.Controls;

public class GraphNodeItemView : ListBoxItem
{
    private bool isLeftMouseButtonDown;
    private Point startMousePosition;

    private GraphControl? ParentGraphControl => this.FindAncestorOfType<GraphControl>();

    public void BringToFront()
    {
        if (ParentGraphControl == null)
            return;

        var maxZ = ParentGraphControl.GetMaxZIndex();
        ZIndex = maxZ + 1;
    }

    #region Dependency properties

    public static readonly StyledProperty<double> XProperty = AvaloniaProperty.Register<GraphNodeItemView, double>(nameof(X),
        defaultBindingMode: BindingMode.TwoWay);

    public double X
    {
        get => GetValue(XProperty);
        set => SetValue(XProperty, value);
    }

    public static readonly StyledProperty<double> YProperty = AvaloniaProperty.Register<GraphNodeItemView, double>(nameof(Y),
        defaultBindingMode: BindingMode.TwoWay);

    public double Y
    {
        get => GetValue(YProperty);
        set => SetValue(YProperty, value);
    }

    public static readonly StyledProperty<bool> IsDraggingProperty = AvaloniaProperty.Register<GraphNodeItemView, bool>(
        nameof(IsDragging),
        defaultBindingMode: BindingMode.TwoWay);

    public bool IsDragging
    {
        get => GetValue(IsDraggingProperty);
        set => SetValue(IsDraggingProperty, value);
    }

    public static readonly StyledProperty<int> ZIndexProperty = AvaloniaProperty.Register<GraphNodeItemView, int>(
        nameof(ZIndex),
        defaultBindingMode: BindingMode.TwoWay);

    public int ZIndex
    {
        get => GetValue(ZIndexProperty);
        set => SetValue(ZIndexProperty, value);
    }

    #endregion

    #region Mouse input

    private double startX;
    private double startY;

    private void DoSelection()
    {
        if (ParentGraphControl == null)
            return;

        ParentGraphControl.ClearSelection();
        IsSelected = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        BringToFront();

        DoSelection();

        if (ParentGraphControl != null)
            startMousePosition = e.GetPosition(ParentGraphControl);

        startX = X;
        startY = Y;

        isLeftMouseButtonDown = true;

        e.Handled = true;


        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (IsDragging)
        {
            var newMousePosition = e.GetPosition(ParentGraphControl);
            Vector delta = newMousePosition - startMousePosition;

            var x = startX + delta.X;
            var y = startY + delta.Y;

            x -= (x + Bounds.Width / 2) % 25;
            y -= y % 25;

            //X = x;
            //Y = y;
            // Avalonia bindings are broken :(
            ((INodeViewModelBase)DataContext!).X = x;
            ((INodeViewModelBase)DataContext).Y = y;
        }

        if (isLeftMouseButtonDown && !IsDragging)
        {
            startX = X;
            startY = Y;
            IsDragging = true;
            //CaptureMouse();
        }

        e.Handled = true;
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (isLeftMouseButtonDown)
        {
            isLeftMouseButtonDown = false;

            if (IsDragging)
                //ReleaseMouseCapture();
                IsDragging = false;
        }

        e.Handled = true;
        base.OnPointerReleased(e);
    }

    #endregion
}