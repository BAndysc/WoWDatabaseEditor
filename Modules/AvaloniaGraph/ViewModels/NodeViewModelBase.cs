using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using Avalonia.Media;
using AvaloniaGraph.Controls;
using AvaloniaGraph.GraphLayout;

namespace AvaloniaGraph.ViewModels;

public interface INodeViewModelBase
{
    public double X { get; set; }
    public double Y { get; set; }
}

public abstract class NodeViewModelBase<T> : ViewModelBase, ITreeNode, INodeViewModelBase where T : NodeViewModelBase<T>
{
    private bool isDragging;
    private bool isSelected;

    private double x;

    private double y;
    private uint entry;

    protected NodeViewModelBase()
    {
        InputConnectors = new ObservableCollection<InputConnectorViewModel<T>>();
        OutputConnectors = new ObservableCollection<OutputConnectorViewModel<T>>();
    }

    public Vector2 Force { get; set; }

    public double X
    {
        get => x;
        set => SetProperty(ref x, value);
    }

    public double Y
    {
        get => y;
        set => SetProperty(ref y, value);
    }

    public bool IsDragging
    {
        get => isDragging;
        set => SetProperty(ref isDragging, value);
    }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public IList<InputConnectorViewModel<T>> InputConnectors { get; }

    public IList<OutputConnectorViewModel<T>> OutputConnectors { get; }

    public IEnumerable<ConnectionViewModel<T>> AttachedConnections
    {
        get
        {
            return InputConnectors.SelectMany(a => a.Connections)
                .Union(OutputConnectors.SelectMany(a => a.Connections));
        }
    }


    public double PerfectX { get; set; }
    public double PerfectY { get; set; }

    public uint Entry
    {
        get => entry;
        set => SetProperty(ref entry, value);
    }

    public int Level { get; set; }

    public abstract TreeNodeIterator ChildrenIterator { get; }

    public double TreeWidth => 100;
    public double TreeHeight => 100;
    public bool Collapsed => false;

    public event EventHandler? OutputChanged;

    protected InputConnectorViewModel<T> AddInputConnector(ConnectorAttachMode attachMode, string name, Color color)
    {
        InputConnectorViewModel<T> inputConnector = new((this as T)!, attachMode, name, color);
        InputConnectors.Add(inputConnector);
        return inputConnector;
    }

    protected OutputConnectorViewModel<T> AddOutputConnector(ConnectorAttachMode attachMode, string name, Color color)
    {
        OutputConnectorViewModel<T> outputConnector = new((this as T)!, attachMode, name, color);
        OutputConnectors.Add(outputConnector);
        return outputConnector;
    }

    protected virtual void RaiseOutputChanged()
    {
        OutputChanged?.Invoke(this, EventArgs.Empty);
    }
}