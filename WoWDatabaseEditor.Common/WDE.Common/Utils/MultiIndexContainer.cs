using System;
using System.Collections.Generic;

namespace WDE.Common.Utils;

public interface IMultiIndexContainer
{
    void Add(int index);
    void Remove(int index);
    bool Contains(int index);
    void Clear();
    IEnumerable<int> All();
    IEfficientContainsIterator ContainsIterator { get; }
}

public interface IEfficientContainsIterator
{
    bool HasMore();
    bool Contains(int index);
}

public class MultiIndexContainer : IMultiIndexContainer
{
    private List<(int start, int end)> ranges = new List<(int start, int end)>();
    //(2,5) (7, 10)

    public IReadOnlyList<(int start, int end)> DebugRanges => ranges;

    private int FindFirstHigherThanEnd(int index)
    {
        int start = 0;
        int end = ranges.Count - 1;
        while (start <= end)
        {
            int mid = (start + end) / 2;
            if (ranges[mid].end < index)
            {
                start = mid + 1;
            }
            else
            {
                end = mid - 1;
            }
        }
        return start;
    }

    private int FindRangeStartHigherOrEquals(int index)
    {
        int start = 0;
        int end = ranges.Count - 1;
        while (start <= end)
        {
            int mid = (start + end) / 2;
            if (ranges[mid].start <= index && ranges[mid].end >= index)
            {
                return mid;
            }
            else if (ranges[mid].start <= index)
            {
                start = mid + 1;
            }
            else
            {
                end = mid - 1;
            }
        }
        return start;
    }
    
    public void Add(int num)
    {
        if (ranges.Count == 0)
            ranges.Add((num, num));
        else
        {
            var indexSmaller = FindFirstHigherThanEnd(num);
            if (indexSmaller == -1)
            {
                if (ranges[0].start == num + 1)
                    ranges[0] = (num, ranges[0].end);
                else if (ranges[0].start > num + 1)
                    ranges.Insert(0, (num, num));
                else
                    throw new Exception();
            }
            else if (indexSmaller > ranges.Count - 1)
            {
                if (ranges[indexSmaller - 1].end == num - 1)
                    ranges[indexSmaller - 1] = (ranges[indexSmaller - 1].start, num);
                else 
                    ranges.Add((num, num));   
            }
            else
            {
                if (ranges[indexSmaller].end + 1 == num)
                {
                    if (indexSmaller == ranges.Count - 1)
                    {
                        ranges[indexSmaller] = (ranges[indexSmaller].start, num);
                    }
                    else if (ranges[indexSmaller + 1].start == num + 1)
                    {
                        ranges[indexSmaller] = (ranges[indexSmaller].start, ranges[indexSmaller + 1].end);
                        ranges.RemoveAt(indexSmaller + 1);
                    }
                    else
                        throw new Exception();
                }
                else if (ranges[indexSmaller].start == num + 1)
                {
                    if (indexSmaller == 0 || ranges[indexSmaller - 1].end < num - 1)
                        ranges[indexSmaller] = (num, ranges[indexSmaller].end);
                    else if (ranges[indexSmaller - 1].end == num - 1)
                    {
                        ranges[indexSmaller] = (ranges[indexSmaller - 1].start, ranges[indexSmaller].end);
                        ranges.RemoveAt(indexSmaller - 1);
                    }
                    else
                        throw new Exception();
                }
                else if (ranges[indexSmaller].start > num + 1)
                {
                    if (indexSmaller == 0 || ranges[indexSmaller - 1].end < num - 1)
                        ranges.Insert(indexSmaller, (num, num));
                    else if (ranges[indexSmaller - 1].end == num - 1)
                    {
                        ranges[indexSmaller - 1] = (ranges[indexSmaller - 1].start, num);
                    }
                    else
                        throw new Exception();
                }
                else
                {
                    // already contains
                }
            }
        }
    }

    public void Remove(int index)
    {
        throw new System.NotImplementedException();
    }

    public bool Contains(int num)
    {
        if (ranges.Count == 0)
            return false;
        var index = FindRangeStartHigherOrEquals(num);
        if (index == -1)
            return false;
        if (index == ranges.Count)
            return ranges[index - 1].end >= num;    
        if (ranges[index].start <= num && ranges[index].end >= num)
            return true;
        return false;
    }

    public void Clear()
    {
        ranges.Clear();
    }

    public IEnumerable<int> All()
    {
        foreach (var range in ranges)
        {
            if (range.start == range.end)
                yield return range.start;
            else
            {
                for (int i = range.start; i <= range.end; ++i)
                    yield return i;
            }
        }
    }

    public IEfficientContainsIterator ContainsIterator => new EfficientContainsIterator(this);

    private class EfficientContainsIterator : IEfficientContainsIterator
    {
        private MultiIndexContainer container;
        private int index;

        public EfficientContainsIterator(MultiIndexContainer container)
        {
            this.container = container;
        }

        public bool HasMore()
        {
            return index < container.ranges.Count;
        }

        public bool Contains(int num)
        {
            if (index == container.ranges.Count)
                return false;
            
            while (index < container.ranges.Count &&
                   container.ranges[index].end < num)
                index++;
            
            return index < container.ranges.Count && container.ranges[index].start <= num && container.ranges[index].end >= num;
        }
    }
}

public static class Extensions
{
    public static void AddRange(this IMultiIndexContainer that, IEnumerable<int> indices)
    {
        foreach (var i in indices)
            that.Add(i);
    }
    
    public static void AddRange(this IMultiIndexContainer that, params int[] indices)
    {
        foreach (var i in indices)
            that.Add(i);
    }
}