using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeminiGraphEditor
{
    public class ConnectorItem : ContentControl
    {
        private bool isDragging;
        private Point lastMousePosition;

        static ConnectorItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectorItem), new FrameworkPropertyMetadata(typeof(ConnectorItem)));
        }

        public ConnectorItem()
        {
            LayoutUpdated += OnLayoutUpdated;
        }

        private GraphControl ParentGraphControl => VisualTreeUtility.FindParent<GraphControl>(this);

        public ElementItem ParentElementItem => VisualTreeUtility.FindParent<ElementItem>(this);

        private void OnLayoutUpdated(object sender, EventArgs e)
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
            GraphControl parentGraphControl = VisualTreeUtility.FindParent<GraphControl>(this);
            if (parentGraphControl == null)
                return;

            Point centerPoint = new Point(ActualWidth / 2, ActualHeight / 2);
            Position = TransformToAncestor(parentGraphControl).Transform(centerPoint);
        }

        #region Dependency properties

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            "Position",
            typeof(Point),
            typeof(ConnectorItem));

        public Point Position
        {
            get => (Point) GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        #endregion

        #region Routed events

        public static readonly RoutedEvent ConnectorDragStartedEvent = EventManager.RegisterRoutedEvent("ConnectorDragStarted",
            RoutingStrategy.Bubble,
            typeof(ConnectorItemDragStartedEventHandler),
            typeof(ConnectorItem));

        public static readonly RoutedEvent ConnectorDraggingEvent = EventManager.RegisterRoutedEvent("ConnectorDragging",
            RoutingStrategy.Bubble,
            typeof(ConnectorItemDraggingEventHandler),
            typeof(ConnectorItem));

        public static readonly RoutedEvent ConnectorDragCompletedEvent = EventManager.RegisterRoutedEvent("ConnectorDragCompleted",
            RoutingStrategy.Bubble,
            typeof(ConnectorItemDragCompletedEventHandler),
            typeof(ConnectorItem));

        #endregion

        #region Mouse input

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            ParentElementItem.Focus();

            lastMousePosition = e.GetPosition(ParentGraphControl);
            e.Handled = true;

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (isDragging)
                {
                    Point currentMousePosition = e.GetPosition(ParentGraphControl);
                    Vector offset = currentMousePosition - lastMousePosition;

                    lastMousePosition = currentMousePosition;

                    RaiseEvent(new ConnectorItemDraggingEventArgs(ConnectorDraggingEvent, this, offset.X, offset.Y));
                }
                else
                {
                    ConnectorItemDragStartedEventArgs eventArgs =
                        new ConnectorItemDragStartedEventArgs(ConnectorDragStartedEvent, this);
                    RaiseEvent(eventArgs);

                    if (eventArgs.Cancel)
                        return;

                    isDragging = true;
                    CaptureMouse();
                }

                e.Handled = true;
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                RaiseEvent(new ConnectorItemDragCompletedEventArgs(ConnectorDragCompletedEvent, this));
                ReleaseMouseCapture();
                isDragging = false;
                e.Handled = true;
            }

            base.OnMouseLeftButtonUp(e);
        }

        #endregion
    }
}