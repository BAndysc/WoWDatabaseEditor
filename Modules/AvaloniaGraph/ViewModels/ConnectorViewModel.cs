using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaGraph.Controls;

namespace AvaloniaGraph.ViewModels;

public abstract class ConnectorViewModel<T, K> : ViewModelBase where T : NodeViewModelBase<T, K> where K : ConnectionViewModel<T, K>
{
    private Point position;

    protected ConnectorViewModel(T node, ConnectorAttachMode attachMode, string name, Color color)
    {
        Node = node;
        Name = name;
        Color = color;
        AttachMode = attachMode;
    }

    public T Node { get; }

    public string Name { get; }

    public Color Color { get; }

    public abstract bool NonEmpty { get; }

    public Point Position
    {
        get => position;
        set
        {
            SetProperty(ref position, value);
            RaisePositionChanged();
        }
    }
    
    public ConnectorAttachMode AttachMode { get; }

    public abstract ConnectorDirection ConnectorDirection { get; }

    public event EventHandler? PositionChanged;

    private void RaisePositionChanged()
    {
        PositionChanged?.Invoke(this, EventArgs.Empty);
    }
}