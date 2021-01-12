using System.Windows;

namespace GeminiGraphEditor
{
    public abstract class ConnectionDragEventArgs : RoutedEventArgs
    {
        protected ConnectionDragEventArgs(RoutedEvent routedEvent,
            object source,
            ElementItem elementItem,
            ConnectorItem sourceConnectorItem) : base(routedEvent, source)
        {
            ElementItem = elementItem;
            SourceConnector = sourceConnectorItem;
        }

        public ElementItem ElementItem { get; }

        public ConnectorItem SourceConnector { get; }
    }
}