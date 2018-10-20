using System.Windows;

namespace GeminiGraphEditor
{
    public class ConnectorItemDragStartedEventArgs : RoutedEventArgs
    {
        public bool Cancel { get; set; }

        public ConnectorItemDragStartedEventArgs(RoutedEvent routedEvent, object source) 
            : base(routedEvent, source)
        {
        }
    }

    public delegate void ConnectorItemDragStartedEventHandler(object sender, ConnectorItemDragStartedEventArgs e);
}