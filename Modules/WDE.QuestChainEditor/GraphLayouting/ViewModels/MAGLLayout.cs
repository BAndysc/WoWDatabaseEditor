using System.Collections.Generic;
using System.Threading;
using GraphX.Common.Interfaces;
using GraphX.Measure;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using QuickGraph;
using Point = GraphX.Measure.Point;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public class MAGLLayout<TVertex, TEdge, TGraph> : ILayoutAlgorithm<TVertex, TEdge, TGraph>
    where TVertex : class
    where TEdge : IEdge<TVertex>
    where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
{
    private readonly SugiyamaLayoutSettings settings;

    public MAGLLayout(TGraph visitedGraph,
        IDictionary<TVertex, Point> vertexPositions,
        IDictionary<TVertex, Size> vertexSizes,
        SugiyamaLayoutSettings settings)
    {
        this.settings = settings;
        VisitedGraph = visitedGraph;
        VertexPositions = vertexPositions;
        VertexSizes = vertexSizes;
    }

    public void Compute(CancellationToken cancellationToken)
    {
        GeometryGraph graph = new GeometryGraph();
        Dictionary<TVertex, Node> nodes = new();
        foreach (var vert in VisitedGraph.Vertices)
        {
            VertexPositions.TryGetValue(vert, out var pos);
            VertexSizes.TryGetValue(vert, out var size);
            var node = new Node(new RoundedRect(
                new Rectangle(pos.X, pos.Y, new Microsoft.Msagl.Core.Geometry.Point(size.Width, size.Height)), 0, 0));
            graph.Nodes.Add(node);
            nodes[vert] = node;
        }

        foreach (var edge in VisitedGraph.Edges)
        {
            var source = nodes[edge.Source];
            var target = nodes[edge.Target];
            var edgeGeom = new Edge(source, target);
            graph.Edges.Add(edgeGeom);
        }
        var ll = new LayeredLayout(graph, settings);
        ll.Run();
        foreach (var vert in VisitedGraph.Vertices)
        {
            var node = nodes[vert];
            VertexPositions[vert] = new Point(node.BoundingBox.Center.X, -node.BoundingBox.Center.Y);
        }
    }

    public IDictionary<TVertex, Point> VertexPositions { get; }
    public IDictionary<TVertex, Size> VertexSizes { get; set; }
    public bool NeedVertexSizes { get; set; }
    public bool SupportsObjectFreeze { get; set; }
    public void ResetGraph(IEnumerable<TVertex> vertices, IEnumerable<TEdge> edges)
    {
        throw new System.NotImplementedException();
    }

    public TGraph VisitedGraph { get; }
}