using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaGraph.Controls;

public class ConnectorItemDragStartedEventArgs : ConnectorItemBaseEventArgs
{
    public ConnectorItemDragStartedEventArgs(RoutedEvent routedEvent,
        ConnectorItem source, PointerEventArgs baseArgs) : base(routedEvent, source, baseArgs)
    {
        SourceConnector = source;
    }

    public ConnectorItem SourceConnector { get; }
    public bool Cancel { get; set; }
}

public delegate void ConnectorItemDragStartedEventHandler(object sender, ConnectorItemDragStartedEventArgs e);