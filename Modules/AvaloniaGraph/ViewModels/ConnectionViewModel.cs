using System;
using Avalonia;
using AvaloniaGraph.Controls;
using AvaloniaGraph.GraphLayout;

namespace AvaloniaGraph.ViewModels;

public interface IConnectionViewModel
{
    ITreeNode? ToNode { get; }
}

public class ConnectionViewModel<T, K> : ViewModelBase, IConnectionViewModel where T : NodeViewModelBase<T, K> where K : ConnectionViewModel<T, K>
{
    private OutputConnectorViewModel<T, K>? from;

    private Point fromPosition;

    private bool isSelected;

    private InputConnectorViewModel<T, K>? to;

    private Point toPosition;

    public ConnectionViewModel(OutputConnectorViewModel<T, K> from, InputConnectorViewModel<T, K> to)
    {
        From = from;
        To = to;
    }

    public ConnectionViewModel(InputConnectorViewModel<T, K> to)
    {
        To = to;
    }

    public ConnectionViewModel(OutputConnectorViewModel<T, K> from)
    {
        From = from;
    }

    public OutputConnectorViewModel<T, K>? From
    {
        get => from;
        set
        {
            if (from != null)
            {
                from.PositionChanged -= OnFromPositionChanged;
                from.Connections.Remove((this as K)!);
            }

            from = value;

            if (from != null)
            {
                from.PositionChanged += OnFromPositionChanged;
                from.Connections.Add((this as K)!);
                FromPosition = from.Position;
                // why?
                //To = to;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(StartAttachMode));
        }
    }

    public InputConnectorViewModel<T, K>? To
    {
        get => to;
        set
        {
            if (to != null)
            {
                to.PositionChanged -= OnToPositionChanged;
                to.Connections.Remove((this as K)!);
            }

            to = value;

            if (to != null)
            {
                to.PositionChanged += OnToPositionChanged;
                to.Connections.Add((this as K)!);
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
            From.Connections.Remove((this as K)!);

        if (To != null)
            To.Connections.Remove((this as K)!);
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