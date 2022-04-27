using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaGraph.Controls;

public enum ConnectorAttachMode
{
    Top,
    Bottom,
    Middle,
    Left,
    Right
}

public class ConnectorItem : TemplatedControl
{
    private bool isDragging;
    private Point lastMousePosition;

    public ConnectorItem()
    {
        LayoutUpdated += OnLayoutUpdated;
    }

    private GraphControl? ParentGraphControl => this.FindAncestorOfType<GraphControl>();
    
    private Canvas? ParentCanvas => this.FindAncestorOfType<Canvas>();

    public GraphNodeItemView? ParentElementItem => this.FindAncestorOfType<GraphNodeItemView>();

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        UpdatePosition();
    }

    /// <summary>
    ///     Computes the coordinates, relative to the parent <see cref="GraphControl" />, of this connector.
    ///     This is used to correctly position any connections that may be connected to this connector.
    ///     (Say that 10 times fast.)
    /// </summary>
    private void UpdatePosition()
    {
        if (ParentElementItem == null)
            return;

        var centerPoint = new Point(Bounds.Width / 2, Bounds.Height / 2);
        if (attachMode == ConnectorAttachMode.Top)
            centerPoint = new Point(Bounds.Width / 2, 0);
        else if (attachMode == ConnectorAttachMode.Bottom)
            centerPoint = new Point(Bounds.Width / 2, Bounds.Height);
        else if (attachMode == ConnectorAttachMode.Left)
            centerPoint = new Point(0, Bounds.Height / 2);
        else if (attachMode == ConnectorAttachMode.Right)
            centerPoint = new Point(Bounds.Width, Bounds.Height / 2);
        Position = this.TranslatePoint(centerPoint, ParentCanvas) ?? centerPoint;
    }

    #region Dependency properties

    public static readonly StyledProperty<Point> PositionProperty =
        AvaloniaProperty.Register<ConnectorItem, Point>(nameof(Position));

    public Point Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public ConnectorAttachMode AttachMode
    {
        get => attachMode;
        set => SetAndRaise(AttachModeProperty, ref attachMode, value);
    }

    #endregion

    #region Routed events

    public static readonly RoutedEvent<ConnectorItemDragStartedEventArgs> ConnectorDragStartedEvent =
        RoutedEvent.Register<ConnectorItem, ConnectorItemDragStartedEventArgs>("ConnectorDragStarted",
            RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ConnectorItemDraggingEventArgs> ConnectorDraggingEvent =
        RoutedEvent.Register<ConnectorItem, ConnectorItemDraggingEventArgs>("ConnectorDragging",
            RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ConnectorItemDragCompletedEventArgs> ConnectorDragCompletedEvent =
        RoutedEvent.Register<ConnectorItem, ConnectorItemDragCompletedEventArgs>("ConnectorDragCompleted",
            RoutingStrategies.Bubble);

    private ConnectorAttachMode attachMode;

    public static readonly DirectProperty<ConnectorItem, ConnectorAttachMode> AttachModeProperty =
        AvaloniaProperty.RegisterDirect<ConnectorItem, ConnectorAttachMode>("AttachMode", o => o.AttachMode,
            (o, v) => o.AttachMode = v);

    #endregion

    #region Mouse input

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ParentElementItem?.Focus();

        lastMousePosition = e.GetPosition(ParentGraphControl);
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (isDragging)
            {
                var currentMousePosition = e.GetPosition(ParentGraphControl);
                Vector offset = currentMousePosition - lastMousePosition;

                lastMousePosition = currentMousePosition;

                RaiseEvent(new ConnectorItemDraggingEventArgs(ConnectorDraggingEvent, this, offset.X, offset.Y, e));
            }
            else
            {
                var eventArgs =
                    new ConnectorItemDragStartedEventArgs(ConnectorDragStartedEvent, this, e);
                RaiseEvent(eventArgs);

                if (eventArgs.Cancel)
                    return;

                isDragging = true;
                //CaptureMouse();
            }

            e.Handled = true;
        }

        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (isDragging)
        {
            RaiseEvent(new ConnectorItemDragCompletedEventArgs(ConnectorDragCompletedEvent, this, e));
            //ReleaseMouseCapture();
            isDragging = false;
            e.Handled = true;
        }

        base.OnPointerReleased(e);
    }

    #endregion
}