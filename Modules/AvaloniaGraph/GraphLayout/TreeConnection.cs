using System.Collections.Generic;

namespace AvaloniaGraph.GraphLayout;

public struct TreeConnection
{
    public ITreeNode IgnParent { get; }
    public ITreeNode IgnChild { get; }
    public List<DPoint> LstPt { get; }

    public TreeConnection(ITreeNode ignParent, ITreeNode ignChild, List<DPoint> lstPt) : this()
    {
        IgnChild = ignChild;
        IgnParent = ignParent;
        LstPt = lstPt;
    }
}