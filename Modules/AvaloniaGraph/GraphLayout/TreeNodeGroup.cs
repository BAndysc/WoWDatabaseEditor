using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AvaloniaGraph.GraphLayout;

public class TreeNodeGroup : IEnumerable<ITreeNode>
{
    private readonly Collection<ITreeNode> _col = new();

    public int Count => _col.Count;

    public ITreeNode this[int index] => _col[index];

    #region IEnumerable<IGraphNode> Members

    public IEnumerator<ITreeNode> GetEnumerator()
    {
        return _col.GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _col.GetEnumerator();
    }

    #endregion

    public void Add(ITreeNode tn)
    {
        _col.Add(tn);
    }

    internal ITreeNode LeftMost()
    {
        return _col.First();
    }

    internal ITreeNode RightMost()
    {
        return _col.Last();
    }
}