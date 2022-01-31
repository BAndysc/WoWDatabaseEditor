using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaGraph.Controls;

public class ConnectionDragStartedEventArgs : ConnectionDragEventArgs
{
    public ConnectionDragStartedEventArgs(RoutedEvent routedEvent,
        GraphControl source,
        GraphNodeItemView? elementItem,
        ConnectorItem connectorItem,
        PointerEventArgs baseArgs) : base(routedEvent, source, elementItem, connectorItem, baseArgs)
    {
        SourceGraph = source;
    }

    public GraphControl SourceGraph { get; }

    public object? Connection { get; set; }
}

public delegate void ConnectionDragStartedEventHandler(object sender, ConnectionDragStartedEventArgs e);