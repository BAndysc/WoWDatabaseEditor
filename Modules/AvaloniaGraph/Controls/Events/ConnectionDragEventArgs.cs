using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaGraph.Controls;

public abstract class ConnectionDragEventArgs : RoutedEventArgs
{
    protected ConnectionDragEventArgs(RoutedEvent routedEvent,
        Interactive? source,
        GraphNodeItemView? elementItem,
        ConnectorItem sourceConnectorItem,
        PointerEventArgs baseArgs) : base(routedEvent, source)
    {
        ElementItem = elementItem;
        SourceConnector = sourceConnectorItem;
        BaseArgs = baseArgs;
    }

    public PointerEventArgs BaseArgs { get; }
    public GraphNodeItemView? ElementItem { get; }
    public ConnectorItem SourceConnector { get; }
    
    
    public IPointer Pointer => BaseArgs.Pointer;
    public ulong Timestamp => BaseArgs.Timestamp;
    public KeyModifiers KeyModifiers  => BaseArgs.KeyModifiers;
}