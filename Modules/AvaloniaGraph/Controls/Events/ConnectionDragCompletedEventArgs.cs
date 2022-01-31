using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaGraph.Controls;

public class ConnectionDragCompletedEventArgs : ConnectionDragEventArgs
{
    public ConnectionDragCompletedEventArgs(RoutedEvent routedEvent,
        IInteractive? source,
        GraphNodeItemView? elementItem,
        object connection,
        ConnectorItem sourceConnectorItem,
        PointerEventArgs baseArgs) : base(routedEvent, source, elementItem, sourceConnectorItem, baseArgs)
    {
        Connection = connection;
    }

    public object Connection { get; }
}

public delegate void ConnectionDragCompletedEventHandler(object sender, ConnectionDragCompletedEventArgs e);