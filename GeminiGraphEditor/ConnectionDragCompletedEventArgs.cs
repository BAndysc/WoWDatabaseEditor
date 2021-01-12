using System.Windows;

namespace GeminiGraphEditor
{
    public class ConnectionDragCompletedEventArgs : ConnectionDragEventArgs
    {
        public ConnectionDragCompletedEventArgs(RoutedEvent routedEvent,
            object source,
            ElementItem elementItem,
            object connection,
            ConnectorItem sourceConnectorItem) : base(routedEvent, source, elementItem, sourceConnectorItem)
        {
            Connection = connection;
        }

        public object Connection { get; }
    }

    public delegate void ConnectionDragCompletedEventHandler(object sender, ConnectionDragCompletedEventArgs e);
}