using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaGraph.Controls;

public class ConnectorItemDraggingEventArgs : ConnectorItemBaseEventArgs
{
    public ConnectorItemDraggingEventArgs(RoutedEvent routedEvent,
        IInteractive? source,
        double horizontalChange,
        double verticalChange,
        PointerEventArgs baseArgs) :
        base(routedEvent, source, baseArgs)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
    }

    public double HorizontalChange { get; }

    public double VerticalChange { get; }
}

public delegate void ConnectorItemDraggingEventHandler(object sender, ConnectorItemDraggingEventArgs e);