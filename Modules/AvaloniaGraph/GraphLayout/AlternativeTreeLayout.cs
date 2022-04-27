using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;

namespace AvaloniaGraph.GraphLayout;

public class AlternativeTreeLayout
{
    private float NodeWidth = 140;
    private float NodeSpacing = 20;

    private HashSet<ITreeNode> visitedTemp = new();
    private Dictionary<ITreeNode, int> widths = new();

    public float GetTotalWidth(int widthNumber)
    {
        if (widthNumber == 0)
            return 0;
        
        if (widthNumber == 1)
            return NodeWidth;

        return NodeWidth * widthNumber + (widthNumber - 1) * NodeSpacing;
    }
    
    public void DoLayout(IReadOnlyList<ITreeNode> roots)
    {
        float startX = 0;
        widths.Clear();
        CalculateWidths(roots);
        ClearLevel(roots);
        foreach (var root in roots)
        {
            BfsXPlacement(root, startX, visitedTemp);
            startX += GetTotalWidth(GetWidth(root));
        }
        visitedTemp.Clear();

        // doing it two times to make sure all nodes are placed good enough
        SetLevel(roots);
        SetLevel(roots);

        SetPerfectY(roots);
    }

    private void SetPerfectY(IReadOnlyList<ITreeNode> roots)
    {
        Queue<ITreeNode> queue = new Queue<ITreeNode>();
        foreach (var root in roots)
            queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            node.PerfectY = node.Level * 120;
            var itr = node.ChildrenIterator;
            while (itr.HasNextChild())
            {
                var child = itr.NextChild();
                queue.Enqueue(child);
            }
        }
    }
    
    private void SetLevel(IReadOnlyList<ITreeNode> roots)
    {
        Queue<(ITreeNode, int)> queue = new Queue<(ITreeNode, int)>();
        foreach (var root in roots)
            queue.Enqueue((root, 0));
        while (queue.Count > 0)
        {
            var (node, level) = queue.Dequeue();
            node.Level = Math.Max(level, node.Level);
            var itr = node.ChildrenIterator;

            int nextLevel = node.Level + 1;
            while (itr.HasNextChild())
            {
                nextLevel = Math.Max(nextLevel, itr.NextChild().Level);
            }

            itr = node.ChildrenIterator;
            while (itr.HasNextChild())
            {
                var child = itr.NextChild();
                queue.Enqueue((child, nextLevel));
            }
        }
    }

    private void ClearLevel(IReadOnlyList<ITreeNode> roots)
    {
        Stack<ITreeNode> stack = new Stack<ITreeNode>();
        foreach (var root in roots)
            stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            node.Level = 0;
            var itr = node.ChildrenIterator;
            while (itr.HasNextChild())
            {
                var child = itr.NextChild();
                stack.Push(child);
            }
        }
    }
    
    private void BfsXPlacement(ITreeNode root, float startX, HashSet<ITreeNode> visited)
    {
        var queue = new Queue<(ITreeNode node, float x)>();
        queue.Enqueue((root, startX));
        while (queue.Count > 0)
        {
            var pair = queue.Dequeue();
            
            if (!visited.Add(pair.node))
                continue;

            var totalWidth = GetTotalWidth(GetWidth(pair.node));
            pair.node.PerfectX = pair.x + totalWidth / 2 - NodeWidth / 2;

            var x = pair.x;
            //pair.node.Entry = (uint)GetWidth(pair.node);
            var itr = pair.node.ChildrenIterator;
            while (itr.HasNextChild())
            {
                var child = itr.NextChild();
                var childWidth = GetTotalWidth(GetWidth(child));
                queue.Enqueue((child, x));
                x += childWidth;
            }
        }
    }

    // returns the width of the tree
    public int GetWidth(ITreeNode? node, HashSet<ITreeNode>? visited = null)
    {
        if (node == null)
            return 0;

        return widths[node];
    }
    
    private void CalculateWidths(IReadOnlyList<ITreeNode> roots)
    {
        int CalculateWidth(ITreeNode? node, HashSet<ITreeNode> visited)
        {
            if (node == null)
                return 0;

            int sum = 0;
            var iterator = node.ChildrenIterator;
            
            while (iterator.HasNextChild())
            {
                var child = iterator.NextChild();
                if (visited.Add(child))
                    sum += CalculateWidth(child, visited);
            }

            return widths[node] = Math.Max(1, sum);
        }
        
        foreach (var root in roots)
        {
            CalculateWidth(root, visitedTemp);
        }
        visitedTemp.Clear();
    }
}