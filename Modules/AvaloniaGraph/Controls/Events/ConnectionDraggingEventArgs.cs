using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaGraph.Controls;

public class ConnectionDraggingEventArgs : ConnectionDragEventArgs
{
    public ConnectionDraggingEventArgs(RoutedEvent routedEvent,
        IInteractive? source,
        GraphNodeItemView? elementItem,
        object connection,
        ConnectorItem connectorItem,
        PointerEventArgs baseArgs) : base(routedEvent, source, elementItem, connectorItem, baseArgs)
    {
        Connection = connection;
    }

    public object Connection { get; }
}

public delegate void ConnectionDraggingEventHandler(object sender, ConnectionDraggingEventArgs e);