using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaGraph.Controls;

public abstract class ConnectorItemBaseEventArgs : RoutedEventArgs
{
    public ConnectorItemBaseEventArgs(RoutedEvent routedEvent,
        Interactive? source,
        PointerEventArgs baseArgs) :
        base(routedEvent, source)
    {
        BaseArgs = baseArgs;
    }

    public PointerEventArgs BaseArgs { get; }

    public IPointer Pointer => BaseArgs.Pointer;
    public ulong Timestamp => BaseArgs.Timestamp;
    public KeyModifiers KeyModifiers  => BaseArgs.KeyModifiers;
}