using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaGraph.Controls;

public class ConnectorItemDragCompletedEventArgs : ConnectorItemBaseEventArgs
{
    public ConnectorItemDragCompletedEventArgs(RoutedEvent routedEvent,
        Interactive? source, PointerEventArgs baseArgs) : base(routedEvent, source, baseArgs)
    {
    }
}

public delegate void ConnectorItemDragCompletedEventHandler(object sender, ConnectorItemDragCompletedEventArgs e);