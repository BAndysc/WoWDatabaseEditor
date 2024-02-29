using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Disposables;

namespace WDE.Common.Utils;

public readonly struct VerticalCursor : IEquatable<VerticalCursor>, IComparable<VerticalCursor>, IComparable
{
    public int CompareTo(VerticalCursor other)
    {
        var groupIndexComparison = GroupIndex.CompareTo(other.GroupIndex);
        if (groupIndexComparison != 0) return groupIndexComparison;
        return RowIndex.CompareTo(other.RowIndex);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is VerticalCursor other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(VerticalCursor)}");
    }

    public static bool operator <(VerticalCursor left, VerticalCursor right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(VerticalCursor left, VerticalCursor right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(VerticalCursor left, VerticalCursor right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(VerticalCursor left, VerticalCursor right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static readonly VerticalCursor None = new VerticalCursor(-1, -1);
    public readonly int GroupIndex;
    public readonly int RowIndex;

    public VerticalCursor(int groupIndex, int rowIndex)
    {
        GroupIndex = groupIndex;
        RowIndex = rowIndex;
    }

    public VerticalCursor WithRowIndex(int rowIndex)
    {
        return new VerticalCursor(GroupIndex, rowIndex);
    }

    public VerticalCursor AddRowIndex(int diff)
    {
        return WithRowIndex(RowIndex + diff);
    }
    
    public VerticalCursor WithGroupIndex(int groupIndex)
    {
        // zero row index on purpose
        return new VerticalCursor(groupIndex, 0);
    }

    public VerticalCursor AddGroupIndex(int diff)
    {
        return WithGroupIndex(GroupIndex + diff);
    }
    
    public bool Equals(VerticalCursor other)
    {
        return GroupIndex == other.GroupIndex && RowIndex == other.RowIndex;
    }

    public override bool Equals(object? obj)
    {
        return obj is VerticalCursor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GroupIndex, RowIndex);
    }

    public static bool operator ==(VerticalCursor left, VerticalCursor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VerticalCursor left, VerticalCursor right)
    {
        return !left.Equals(right);
    }

    public bool IsGroupValid => GroupIndex != -1;
    
    public bool IsValid => IsGroupValid && RowIndex != -1;
}


public interface ITableMultiSelection
{
    void Add(VerticalCursor index);
    void Remove(VerticalCursor index);
    bool Contains(VerticalCursor index);
    void Clear();
    IEnumerable<VerticalCursor> All();
    IEnumerable<VerticalCursor> AllReversed();
    ITableEfficientContainsIterator ContainsIterator { get; }
    bool MoreThanOne { get; }
    bool Empty { get; }
    event Action? SelectionChanged;
    IDisposable PauseNotifications();
    void Set(VerticalCursor index);
    void RemoveGroup(int group);
}

public interface ITableEfficientContainsIterator : System.IDisposable
{
    bool Contains(VerticalCursor index);
}

public class TableMultiSelection : ITableMultiSelection
{
    private SortedDictionary<int, MultiIndexContainer> perGroupContainer =
        new SortedDictionary<int, MultiIndexContainer>();

    private MultiIndexContainer GroupContainer(VerticalCursor index)
    {
        if (!perGroupContainer.TryGetValue(index.GroupIndex, out var groupContainer))
            perGroupContainer[index.GroupIndex] = groupContainer = new MultiIndexContainer();
        return groupContainer;
    }
    
    public void Add(VerticalCursor index)
    {
        GroupContainer(index).Add(index.RowIndex);
        Notify();
    }

    public void Remove(VerticalCursor index)
    {
        if (perGroupContainer.ContainsKey(index.GroupIndex))
        {
            GroupContainer(index).Remove(index.RowIndex);
            Notify();
        }
    }

    public bool Contains(VerticalCursor index)
    {
        if (!perGroupContainer.ContainsKey(index.GroupIndex))
            return false;
        return GroupContainer(index).Contains(index.RowIndex);
    }

    public void Clear()
    {
        perGroupContainer.Clear();
        Notify();
    }

    public IEnumerable<VerticalCursor> All()
    {
        foreach (var group in perGroupContainer)
        {
            foreach (var row in group.Value.All())
                yield return new VerticalCursor(group.Key, row);
        }
    }

    public IEnumerable<VerticalCursor> AllReversed()
    {
        foreach (var group in perGroupContainer.Reverse())
        {
            foreach (var row in group.Value.AllReversed())
                yield return new VerticalCursor(group.Key, row);
        }
    }

    public ITableEfficientContainsIterator ContainsIterator => new TableEfficientContainsIterator(this);

    public bool MoreThanOne => All().Count() > 1;

    public bool Empty => perGroupContainer.Count == 0 || perGroupContainer.All(g => g.Value.Empty);
    public event Action? SelectionChanged;
    private bool pendingNotification;
    private int notificationsBlockedStack = 0;
    
    public IDisposable PauseNotifications()
    {
        notificationsBlockedStack++;
        bool disposed = false;
        return new ActionDisposable(() =>
        {
            if (disposed)
                return;
            disposed = true;
            notificationsBlockedStack--;
            if (notificationsBlockedStack == 0 && pendingNotification)
                SelectionChanged?.Invoke();
        });
    }

    public void Set(VerticalCursor index)
    {
        Clear();
        Add(index);
    }

    public void RemoveGroup(int group)
    {
        perGroupContainer.Remove(group);
    }

    private void Notify()
    {
        if (notificationsBlockedStack > 0)
        {
            pendingNotification = true;
            return;
        }
        SelectionChanged?.Invoke();
    }

    private class TableEfficientContainsIterator : ITableEfficientContainsIterator
    {
        private readonly TableMultiSelection tableMultiSelection;
        private IEfficientContainsIterator? inner;
        private bool disposed = false;
        private SortedDictionary<int, MultiIndexContainer>.Enumerator groupEnumerator;

        public TableEfficientContainsIterator(TableMultiSelection tableMultiSelection)
        {
            this.tableMultiSelection = tableMultiSelection;
            groupEnumerator = this.tableMultiSelection.perGroupContainer.GetEnumerator();
            if (!groupEnumerator.MoveNext())
                Dispose();
            else
                inner = groupEnumerator.Current.Value.ContainsIterator;
        }
        
        public bool Contains(VerticalCursor index)
        {
            if (disposed)
                return false;

            while (groupEnumerator.Current.Key < index.GroupIndex)
            {
                if (!groupEnumerator.MoveNext())
                {
                    Dispose();
                    return false;
                }

                inner = groupEnumerator.Current.Value.ContainsIterator;
            }

            if (groupEnumerator.Current.Key > index.GroupIndex)
                return false;

            return inner!.Contains(index.RowIndex);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                groupEnumerator.Dispose();
                groupEnumerator = default;
                disposed = true;
            }
        }
    }
}