using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaGraph.Controls;

public abstract class ConnectionDragEventArgs : PointerEventArgs
{
    protected ConnectionDragEventArgs(RoutedEvent routedEvent,
        IInteractive? source,
        GraphNodeItemView? elementItem,
        ConnectorItem sourceConnectorItem,
        PointerEventArgs baseArgs) : base(routedEvent, source,
        baseArgs.Pointer,
        (baseArgs.Source as IVisual)?.VisualRoot,
        baseArgs.GetPosition(null),
        baseArgs.Timestamp,
        baseArgs.GetCurrentPoint(null).Properties,
        baseArgs.KeyModifiers)
    {
        ElementItem = elementItem;
        SourceConnector = sourceConnectorItem;
    }

    public GraphNodeItemView? ElementItem { get; }
    public ConnectorItem SourceConnector { get; }
}