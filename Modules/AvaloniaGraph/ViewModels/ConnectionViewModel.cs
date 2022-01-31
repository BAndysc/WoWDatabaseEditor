using System;
using Avalonia;
using AvaloniaGraph.Controls;
using AvaloniaGraph.GraphLayout;

namespace AvaloniaGraph.ViewModels;

public interface IConnectionViewModel
{
    ITreeNode? ToNode { get; }
}

public class ConnectionViewModel<T> : ViewModelBase, IConnectionViewModel where T : NodeViewModelBase<T>
{
    private OutputConnectorViewModel<T>? from;

    private Point fromPosition;

    private bool isSelected;

    private InputConnectorViewModel<T>? to;

    private Point toPosition;

    public ConnectionViewModel(OutputConnectorViewModel<T> from, InputConnectorViewModel<T> to)
    {
        From = from;
        To = to;
    }

    public ConnectionViewModel(InputConnectorViewModel<T> to)
    {
        To = to;
    }

    public ConnectionViewModel(OutputConnectorViewModel<T> from)
    {
        From = from;
    }

    public OutputConnectorViewModel<T>? From
    {
        get => from;
        set
        {
            if (from != null)
            {
                from.PositionChanged -= OnFromPositionChanged;
                from.Connections.Remove(this);
            }

            from = value;

            if (from != null)
            {
                from.PositionChanged += OnFromPositionChanged;
                from.Connections.Add(this);
                FromPosition = from.Position;
                // why?
                //To = to;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(StartAttachMode));
        }
    }

    public InputConnectorViewModel<T>? To
    {
        get => to;
        set
        {
            if (to != null)
            {
                to.PositionChanged -= OnToPositionChanged;
                to.Connections.Remove(this);
            }

            to = value;

            if (to != null)
            {
                to.PositionChanged += OnToPositionChanged;
                to.Connections.Add(this);
                ToPosition = to.Position;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(EndAttachMode));
        }
    }
    
    public ConnectorAttachMode StartAttachMode => From?.AttachMode ?? ConnectorAttachMode.Middle;
    
    public ConnectorAttachMode EndAttachMode => To?.AttachMode ?? ConnectorAttachMode.Middle;

    public Point TopLeftPosition => new(Math.Min(fromPosition.X, toPosition.X), Math.Min(fromPosition.Y, toPosition.Y));

    public Point FromPosition
    {
        get => fromPosition;
        set
        {
            fromPosition = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(TopLeftPosition));
        }
    }

    public Point ToPosition
    {
        get => toPosition;
        set
        {
            toPosition = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(TopLeftPosition));
        }
    }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            RaisePropertyChanged();
        }
    }

    public void Detach()
    {
        if (From != null)
            From.Connections.Remove(this);

        if (To != null)
            To.Connections.Remove(this);
    }

    private void OnFromPositionChanged(object? sender, EventArgs e)
    {
        FromPosition = From!.Position;
    }

    private void OnToPositionChanged(object? sender, EventArgs e)
    {
        ToPosition = To!.Position;
    }

    public ITreeNode? ToNode => To?.Node;
    public T? FromNode => From?.Node;
}