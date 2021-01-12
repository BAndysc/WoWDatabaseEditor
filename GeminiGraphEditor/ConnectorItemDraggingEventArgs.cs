using System.Windows;

namespace GeminiGraphEditor
{
    public class ConnectorItemDraggingEventArgs : RoutedEventArgs
    {
        public ConnectorItemDraggingEventArgs(RoutedEvent routedEvent, object source, double horizontalChange, double verticalChange) :
            base(routedEvent, source)
        {
            HorizontalChange = horizontalChange;
            VerticalChange = verticalChange;
        }

        public double HorizontalChange { get; }

        public double VerticalChange { get; }
    }

    public delegate void ConnectorItemDraggingEventHandler(object sender, ConnectorItemDraggingEventArgs e);
}