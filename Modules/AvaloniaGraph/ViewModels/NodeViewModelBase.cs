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

public abstract class NodeViewModelBase<T, K> : ViewModelBase, ITreeNode, INodeViewModelBase where T : NodeViewModelBase<T, K> where K : ConnectionViewModel<T, K>
{
    private bool isDragging;
    private bool isSelected;

    private double x;

    private double y;
    private uint entry;

    protected NodeViewModelBase()
    {
        InputConnectors = new ObservableCollection<InputConnectorViewModel<T, K>>();
        OutputConnectors = new ObservableCollection<OutputConnectorViewModel<T, K>>();
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

    protected IList<InputConnectorViewModel<T, K>> InputConnectors { get; }

    protected IList<OutputConnectorViewModel<T, K>> OutputConnectors { get; }

    protected IEnumerable<K> AttachedConnections
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

    protected InputConnectorViewModel<T, K> AddInputConnector(ConnectorAttachMode attachMode, string name, Color color)
    {
        InputConnectorViewModel<T, K> inputConnector = new((this as T)!, attachMode, name, color);
        InputConnectors.Add(inputConnector);
        return inputConnector;
    }

    protected OutputConnectorViewModel<T, K> AddOutputConnector(ConnectorAttachMode attachMode, string name, Color color)
    {
        OutputConnectorViewModel<T, K> outputConnector = new((this as T)!, attachMode, name, color);
        OutputConnectors.Add(outputConnector);
        return outputConnector;
    }

    protected virtual void RaiseOutputChanged()
    {
        OutputChanged?.Invoke(this, EventArgs.Empty);
    }
}