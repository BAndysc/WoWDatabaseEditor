using System.Windows;

namespace GeminiGraphEditor
{
    public class ConnectionDraggingEventArgs : ConnectionDragEventArgs
    {
        public ConnectionDraggingEventArgs(RoutedEvent routedEvent,
            object source,
            ElementItem elementItem,
            object connection,
            ConnectorItem connectorItem) : base(routedEvent, source, elementItem, connectorItem)
        {
            Connection = connection;
        }

        public object Connection { get; }
    }

    public delegate void ConnectionDraggingEventHandler(object sender, ConnectionDraggingEventArgs e);
}