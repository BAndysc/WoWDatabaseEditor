using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AvaloniaGraph.ViewModels;

namespace AvaloniaGraph.GraphLayout;

public interface ITreeNodeIterator
{
    bool HasNextChild();
    ITreeNode NextChild();
}


public struct TreeNodeIterator : ITreeNodeIterator
{
    private readonly IList connections;
    private int i = 0;
    private int count = 0;

    public TreeNodeIterator(IList connections)
    {
        this.connections = connections;
        this.count = connections.Count;
    }

    public bool HasNextChild()
    {
        while (i < count && ((connections[i] as IConnectionViewModel)!.ToNode == null))
            i++;
        return i < count;
    }

    public ITreeNode NextChild()
    {
        return ((connections[i++] as IConnectionViewModel)!.ToNode)!;
    }
}

public interface ITreeNode
{
    TreeNodeIterator ChildrenIterator { get; }
    double TreeWidth { get; }
    double TreeHeight { get; }
    bool Collapsed { get; }
    double PerfectX { get; set; }
    double PerfectY { get; set; }
    uint Entry { get; set; }
    int Level { get; set; }
}