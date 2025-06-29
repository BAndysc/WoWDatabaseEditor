using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphX.Logic.Algorithms.LayoutAlgorithms;
using GraphX.Measure;
using QuickGraph;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.GraphLayouting.ViewModels;
using WDE.QuestChainEditor.ViewModels;
using ConnectionViewModel = WDE.QuestChainEditor.ViewModels.ConnectionViewModel;

namespace WDE.QuestChainEditor.GraphLayouting;

[AutoRegister]
[SingleInstance]
public class AutomaticGraphLayouter : IAutomaticGraphLayouter
{
    public AutomaticGraphLayouter()
    {
    }

    private class GraphSubGroup
    {
        public Graph graph = new Graph();
        public Dictionary<BaseQuestViewModel, Size> sizes = new();
        public Dictionary<BaseQuestViewModel, Point> positions = new();
        public Avalonia.Rect bounds;

        public void CalculateBounds()
        {
            bounds = default;
            bool first = true;
            foreach (var node in graph.Vertices)
            {
                if (first)
                {
                    bounds = node.Bounds;
                }
                else
                {
                    bounds = bounds.Union(node.Bounds);
                }
            }
        }
    }

    public async Task DoLayoutImpl(bool synchronous,
        ILayoutAlgorithmProvider algorithm,
        IReadOnlyList<BaseQuestViewModel> nodes,
        IReadOnlyList<ConnectionViewModel> connections,
        bool includeSoloNodes,
        CancellationToken cancellationToken)
    {

        HashSet<BaseQuestViewModel> addedNodes = new();
        BaseQuestViewModel GetNodeOrParentGroup(BaseQuestViewModel node)
        {
            if (node is QuestViewModel quest && quest.ExclusiveGroup != null)
                return quest.ExclusiveGroup;
            return node;
        }

        void AddNode(GraphSubGroup subGroup, BaseQuestViewModel node)
        {
            if (addedNodes.Add(node))
            {
                subGroup.graph.AddVertex(node);
                var size = new Size(Math.Max(node.Bounds.Width, 10), Math.Max(node.Bounds.Height, 10) + (node is ExclusiveGroupViewModel ? 30 : 0));
                subGroup.sizes[node] = size; // the more connections, the more height I want
                subGroup.positions[node] = new Point(node.X + size.Width / 2, node.Y); // centering the node on the X axis to align to center of the node
            }
        }

        if (!algorithm.SeparateByGroups)
        {
            GraphSubGroup subGroup = new();

            foreach (var node in nodes)
            {
                AddNode(subGroup, GetNodeOrParentGroup(node));
            }

            foreach (var conn in connections.Where(c => c.RequirementType != ConnectionType.FactionChange))
            {
                if (conn.FromNode != null && conn.ToNode != null)
                {
                    var from = GetNodeOrParentGroup(conn.FromNode);
                    var to = GetNodeOrParentGroup(conn.ToNode);
                    AddNode(subGroup, from);
                    AddNode(subGroup, to);
                    subGroup.graph.AddEdge(new Edge(from, to));
                }
            }

            /* can't cache this */
            var layoutAlgorithm = algorithm.Create(subGroup.graph,
                subGroup.sizes,
                subGroup.positions);

            if (synchronous)
            {
                layoutAlgorithm.Compute(cancellationToken);
            }
            else
            {
                await Task.Run(() =>
                {
                    layoutAlgorithm.Compute(cancellationToken);
                }, cancellationToken);
            }
            if (cancellationToken.IsCancellationRequested)
                return;

            Rect graphBounds = default;
            foreach (var (vertex, size) in subGroup.sizes)
            {
                if (layoutAlgorithm.VertexPositions.TryGetValue(vertex, out var position))
                {
                    var bounds = new Rect(position.X - size.Width / 2, position.Y, size.Width, size.Height);
                    if (graphBounds == default)
                        graphBounds = bounds;
                    graphBounds = Rect.Union(graphBounds, bounds);
                }
            }

            foreach (var vertex in layoutAlgorithm.VertexPositions)
            {
                if (includeSoloNodes || subGroup.graph.Degree(vertex.Key) != 0)
                {
                    vertex.Key.PerfectX = vertex.Value.X - subGroup.sizes[vertex.Key].Width / 2 - graphBounds.Left;
                    vertex.Key.PerfectY = vertex.Value.Y - graphBounds.Top;
                }
                else
                {
                    // loose vertices shall not move
                    vertex.Key.PerfectX = vertex.Key.Location.X;
                    vertex.Key.PerfectY = vertex.Key.Location.Y;
                }
            }
        }
        else
        {
            HashSet<BaseQuestViewModel> visited = new();
            HashSet<(BaseQuestViewModel, BaseQuestViewModel)> addedEdges = new();

            List<BaseQuestViewModel> disjointGroups = new();
            foreach (var node in nodes)
            {

            }

            void Traverse(GraphSubGroup subGroup, BaseQuestViewModel node)
            {
                node = GetNodeOrParentGroup(node);

                if (!visited.Add(node))
                    return;

                AddNode(subGroup, node);

                foreach (var connection in node.Connector.Connections)
                {
                    if (connection.RequirementType == ConnectionType.FactionChange)
                        continue;

                    if (connection.FromNode == null || connection.ToNode == null)
                        continue;

                    var from = GetNodeOrParentGroup(connection.FromNode);
                    var to = GetNodeOrParentGroup(connection.ToNode);
                    AddNode(subGroup, from);
                    AddNode(subGroup, to);
                    if (addedEdges.Add((from, to)))
                        subGroup.graph.AddEdge(new Edge(from, to));

                    Traverse(subGroup, from);
                    Traverse(subGroup, to);
                }

                if (node is ExclusiveGroupViewModel group)
                {
                    foreach (var quest in group.Quests)
                    {
                        foreach (var connection in quest.Connector.Connections)
                        {
                            if (connection.RequirementType == ConnectionType.FactionChange)
                                continue;

                            if (connection.FromNode == null || connection.ToNode == null)
                                continue;

                            var from = GetNodeOrParentGroup(connection.FromNode);
                            var to = GetNodeOrParentGroup(connection.ToNode);
                            AddNode(subGroup, from);
                            AddNode(subGroup, to);
                            if (addedEdges.Add((from, to)))
                                subGroup.graph.AddEdge(new Edge(from, to));

                            Traverse(subGroup, from);
                            Traverse(subGroup, to);
                        }
                    }
                }
            }

            List<GraphSubGroup> subGroups = new();
            foreach (var node in nodes)
            {
                var nodeToTraverse = GetNodeOrParentGroup(node);

                if (visited.Contains(nodeToTraverse))
                    continue;

                GraphSubGroup subGroup = new();

                Traverse(subGroup, nodeToTraverse);

                subGroup.CalculateBounds();
                subGroups.Add(subGroup);
            }

            subGroups.Sort((a, b) => a.bounds.Center.X.CompareTo(b.bounds.Center.X));

            double xOffset = 0;
            foreach (var subGroup in subGroups)
            {
                /* can't cache this */
                var layoutAlgorithm = algorithm.Create(subGroup.graph,
                    subGroup.sizes,
                    subGroup.positions);

                if (synchronous)
                {
                    layoutAlgorithm.Compute(cancellationToken);
                }
                else
                {
                    await Task.Run(() =>
                    {
                        layoutAlgorithm.Compute(cancellationToken);
                    }, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                Rect graphBounds = default;
                foreach (var (vertex, size) in subGroup.sizes)
                {
                    if (layoutAlgorithm.VertexPositions.TryGetValue(vertex, out var position))
                    {
                        var bounds = new Rect(position.X - size.Width / 2, position.Y, size.Width, size.Height);
                        if (graphBounds == default)
                            graphBounds = bounds;
                        graphBounds = Rect.Union(graphBounds, bounds);
                    }
                }

                foreach (var vertex in subGroup.graph.Vertices)
                {
                    if (includeSoloNodes || subGroup.graph.Degree(vertex) != 0)
                    {
                        if (layoutAlgorithm.VertexPositions.TryGetValue(vertex, out var position))
                        {
                            vertex.PerfectX = xOffset + position.X - subGroup.sizes[vertex].Width / 2 - graphBounds.Left;
                            vertex.PerfectY = position.Y - graphBounds.Top;
                        }
                    }
                    else
                    {
                        vertex.PerfectX = vertex.Location.X;
                        vertex.PerfectY = vertex.Location.Y;
                    }
                }

                foreach (var group in subGroup.graph.Vertices
                             .Select(x => x as ExclusiveGroupViewModel)
                             .Where(g => g != null)
                             .Cast<ExclusiveGroupViewModel>())
                {
                    group.Quests.Sort((a, b) =>
                    {
                        var a_x = a?.Prerequisites.Select(x => (double?)x.prerequisite.PerfectX).FirstOrDefault((double?)null) ?? a?.PerfectX ?? 0;
                        var b_x = b?.Prerequisites.Select(x => (double?)x.prerequisite.PerfectX).FirstOrDefault((double?)null) ?? b?.PerfectY ?? 0;
                        return a_x.CompareTo(b_x);
                    });
                }

                xOffset += graphBounds.Width;
            }
        }
    }

    public async Task DoLayout(ILayoutAlgorithmProvider algorithm, IReadOnlyList<BaseQuestViewModel> nodes, IReadOnlyList<ConnectionViewModel> connections, CancellationToken cancellationToken)
    {
        await DoLayoutImpl(false, algorithm, nodes, connections, false, cancellationToken);
    }

    public void DoLayoutNow(ILayoutAlgorithmProvider algorithm, IReadOnlyList<BaseQuestViewModel> nodes, IReadOnlyList<ConnectionViewModel> connections, bool includeSoloNodes)
    {
        DoLayoutImpl(true, algorithm, nodes, connections, includeSoloNodes, default).Wait(); // when synchronous is true, we can wait
    }

    public class Edge : IEdge<BaseQuestViewModel>
    {
        public Edge(BaseQuestViewModel source, BaseQuestViewModel target)
        {
            Source = source;
            Target = target;
        }

        public BaseQuestViewModel Source { get; }
        public BaseQuestViewModel Target { get; }
    }

    public class Graph : BidirectionalGraph<BaseQuestViewModel, Edge>
    {
    }
}