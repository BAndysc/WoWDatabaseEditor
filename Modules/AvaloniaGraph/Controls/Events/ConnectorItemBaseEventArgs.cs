using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaGraph.Controls;

public abstract class ConnectorItemBaseEventArgs : PointerEventArgs
{
    public ConnectorItemBaseEventArgs(RoutedEvent routedEvent,
        IInteractive? source,
        PointerEventArgs baseArgs) :
        base(routedEvent, source,
            baseArgs.Pointer,
            (baseArgs.Source as IVisual)?.VisualRoot,
            baseArgs.GetPosition(null),
            baseArgs.Timestamp,
            baseArgs.GetCurrentPoint(null).Properties,
            baseArgs.KeyModifiers)
    {
        BaseArgs = baseArgs;
    }

    public PointerEventArgs BaseArgs { get; }
}