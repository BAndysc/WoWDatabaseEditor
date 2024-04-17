using System.Collections.Generic;
using System.Threading;
using GraphX.Common.Interfaces;
using GraphX.Measure;
using QuickGraph;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

internal class NullLayoutAlgorithm<TVertex, TEdge, TGraph> : ILayoutAlgorithm<TVertex, TEdge, TGraph>
    where TVertex : class
    where TEdge : IEdge<TVertex>
    where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
{
    public NullLayoutAlgorithm(TGraph visitedGraph)
    {
        VisitedGraph = visitedGraph;
    }

    public void Compute(CancellationToken cancellationToken) { }

    public IDictionary<TVertex, Point> VertexPositions { get; } = new Dictionary<TVertex, Point>();
    public IDictionary<TVertex, Size> VertexSizes { get; set; } = new Dictionary<TVertex, Size>();
    public bool NeedVertexSizes => false;
    public bool SupportsObjectFreeze => false;
    public void ResetGraph(IEnumerable<TVertex> vertices, IEnumerable<TEdge> edges) { }
    public TGraph VisitedGraph { get; }
}