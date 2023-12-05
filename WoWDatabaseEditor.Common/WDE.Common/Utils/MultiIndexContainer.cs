using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WDE.Common.Utils;

public interface IMultiIndexContainer
{
    void Add(int index);
    void Remove(int index);
    bool Contains(int index);
    void Clear();
    void Replace(int newStart, int newEnd);
    IEnumerable<int> All();
    IEnumerable<int> AllReversed();
    IEfficientContainsIterator ContainsIterator { get; }
    bool MoreThanOne { get; }
    bool Empty { get; }

    event Action? Cleared;
    event Action<int>? Added;
    event Action<int>? Removed;
    event Action? Changed;
}

public interface IEfficientContainsIterator
{
    bool HasMore();
    bool Contains(int index);
}

public class MultiIndexContainer : IMultiIndexContainer
{
    private List<(int start, int end)> ranges = new List<(int start, int end)>();
 
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
        {
            ranges.Add((num, num));
            Added?.Invoke(num);
            Changed?.Invoke();
        }
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
                Added?.Invoke(num);
                Changed?.Invoke();
            }
            else if (indexSmaller > ranges.Count - 1)
            {
                if (ranges[indexSmaller - 1].end == num - 1)
                    ranges[indexSmaller - 1] = (ranges[indexSmaller - 1].start, num);
                else 
                    ranges.Add((num, num));
                Added?.Invoke(num);
                Changed?.Invoke();
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
                    Added?.Invoke(num);
                    Changed?.Invoke();
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
                    Added?.Invoke(num);
                    Changed?.Invoke();
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
                    Added?.Invoke(num);
                    Changed?.Invoke();
                }
                else
                {
                    // already contains
                }
            }
        }
    }

    public void Remove(int num)
    {
        if (ranges.Count == 0)
            return;
        
        var index = FindRangeStartHigherOrEquals(num);
        if (index == -1 || index == ranges.Count || ranges[index].start > num)
            return;

        if (ranges[index].start == num)
        {
            if (ranges[index].end == num)
                ranges.RemoveAt(index);
            else
                ranges[index] = (ranges[index].start + 1, ranges[index].end);
        }
        else if (ranges[index].end == num)
        {
            Debug.Assert(ranges[index].start != num); // handled in the previous case
            ranges[index] = (ranges[index].start, ranges[index].end - 1);
        }
        else
        {
            var start = ranges[index].start;
            var end = ranges[index].end;
            ranges[index] = (start, num - 1);
            ranges.Insert(index + 1, (num + 1, end));
        }
        Removed?.Invoke(num);
        Changed?.Invoke();
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
        Cleared?.Invoke();
        Changed?.Invoke();
    }
    
    public void Replace(int newStart, int newEnd)
    {
        ranges.Clear();
        Cleared?.Invoke();
        ranges.Add((newStart, newEnd));
        if (Added != null)
        {
            for (int i = newStart; i <= newEnd; ++i)
                Added(i);
        }
        Changed?.Invoke();
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

    public IEnumerable<int> AllReversed()
    {
        for (var index = ranges.Count - 1; index >= 0; index--)
        {
            var range = ranges[index];
            if (range.start == range.end)
                yield return range.start;
            else
            {
                for (int i = range.end; i >= range.start; --i)
                    yield return i;
            }
        }
    }

    public IEfficientContainsIterator ContainsIterator => new EfficientContainsIterator(this);

    public bool MoreThanOne => ranges.Count > 1 || (ranges.Count == 1 && ranges[0].start != ranges[0].end);
    public bool Empty => ranges.Count == 0;
    public event Action? Cleared;
    public event Action<int>? Added;
    public event Action<int>? Removed;
    public event Action? Changed;

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