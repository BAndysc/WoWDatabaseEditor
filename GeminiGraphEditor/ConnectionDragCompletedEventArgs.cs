using System.Windows;

namespace GeminiGraphEditor
{
    public class ConnectionDragCompletedEventArgs : ConnectionDragEventArgs
    {
        private readonly object _connection;

        public object Connection
        {
            get { return _connection; }
        }

        public ConnectionDragCompletedEventArgs(RoutedEvent routedEvent, object source, 
            ElementItem elementItem, object connection, ConnectorItem sourceConnectorItem)
            : base(routedEvent, source, elementItem, sourceConnectorItem)
        {
            _connection = connection;
        }
    }

    public delegate void ConnectionDragCompletedEventHandler(object sender, ConnectionDragCompletedEventArgs e);
}