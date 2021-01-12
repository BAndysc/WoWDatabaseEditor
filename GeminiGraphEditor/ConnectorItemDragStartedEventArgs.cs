using System.Windows;

namespace GeminiGraphEditor
{
    public class ConnectorItemDragStartedEventArgs : RoutedEventArgs
    {
        public ConnectorItemDragStartedEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
        }

        public bool Cancel { get; set; }
    }

    public delegate void ConnectorItemDragStartedEventHandler(object sender, ConnectorItemDragStartedEventArgs e);
}