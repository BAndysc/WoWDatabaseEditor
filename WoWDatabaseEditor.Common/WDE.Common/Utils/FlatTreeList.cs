using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using DynamicData.Binding;

namespace WDE.Common.Utils;

public class FlatTreeList<P, C> : IDisposable, IEnumerable, IEnumerable<INodeType>, IList<INodeType>, IReadOnlyList<INodeType>, INotifyCollectionChanged where P : IParentType where C : IChildType
{
    private ObservableCollectionExtended<INodeType> innerList = new();
    private List<P> parents = new();
    private IReadOnlyList<P> roots;
    // in order to keep expanded state in sync, we need to keep track of expanded parents here, not by checking IsExpanded
    // because if there is an event bound to IsExpanded changes that changes the children, then this change can be fired before IsExpanded propery change
    // is fired here
    private HashSet<IParentType> expandedParents = new();

    public FlatTreeList(IReadOnlyList<P> roots)
    {
        this.roots = roots;
        InstallParents(roots, 1);
        if (roots is INotifyCollectionChanged incc)
            incc.CollectionChanged += OnParentCollectionChanged;
    }

    private void InstallParents(IReadOnlyList<P> parents, uint level)
    {
        foreach (var parent in parents)
        {
            InstallParent(parent, null, level); 
        }
    }

    private void UninstallParent(P parent, int index)
    {
        if (parent.IsExpanded)
        {
            foreach (var nested in parent.NestedParents)
            {
                UninstallParent((P)nested, index);
            }
            foreach (var child in parent.Children)
            {
                innerList.RemoveAt(index);
            }
        }

        expandedParents.Remove(parent);
        parent.PropertyChanged -= OnParentPropChanged;
        parent.ChildrenChanged -= OnChildrenChanged;
        innerList.RemoveAt(index);
        parents.Remove(parent);
    }

    private int InstallParent(P parent, int? index = null, uint level = 0)
    {
        parent.NestLevel = level;
        int addedElements = 1;
        innerList.Insert(index ?? innerList.Count, parent);
        if (parent.IsExpanded)
        {
            expandedParents.Add(parent);
            foreach (var nestedParent in parent.NestedParents)
            {
                addedElements += InstallParent((P)nestedParent, index + addedElements, level + 1);
            }
            foreach (var child in parent.Children)
            {
                child.NestLevel = level + 1;
                innerList.Insert(index + addedElements ?? innerList.Count, child);
                addedElements++;
            }
        }
        this.parents.Add(parent);
        parent.PropertyChanged += OnParentPropChanged;
        parent.ChildrenChanged += OnChildrenChanged;
        parent.NestedParentsChanged += OnNestedParentsChanged;
        return addedElements;
    }

    private void OnParentCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var parent in parents)
            {
                parent.PropertyChanged -= OnParentPropChanged;
                parent.ChildrenChanged -= OnChildrenChanged;
                parent.NestedParentsChanged -= OnNestedParentsChanged;
            }
            parents.Clear();
            innerList.Clear();
            InstallParents((sender as IReadOnlyList<P>)!, 1);
        }
        else if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems is IReadOnlyList<P> pList)
                InstallParents(pList, 1);
            else
                foreach (P p in e.NewItems!)
                    InstallParent(p, null, 1);
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (P oldParent in e.OldItems!)
            {
                var startIndex = innerList.IndexOf(oldParent);
                UninstallParent(oldParent, startIndex);
            }
        }
        SourceCollectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnParentPropChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IParentType.IsExpanded))
            return;
        
        P parent = (P)sender!;
        var startIndex = innerList.IndexOf(parent);
        if (parent.IsExpanded)
        {
            if (expandedParents.Add(parent))
            {
                int i = 0;

                if (parent.NestedParents.Count > 0)
                {
                    var bulk = innerList.SuspendNotifications();
                    foreach (var nestedParent in parent.NestedParents)
                    {
                        i += InstallParent((P)nestedParent, startIndex + i + 1, parent.NestLevel + 1);
                    }
                    bulk.Dispose();
                    //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)parent.Children, startIndex + 1));
                }
                if (parent.Children.Count > 0)
                {
                    var bulk = innerList.SuspendNotifications();
                    foreach (var child in parent.Children)
                    {
                        child.NestLevel = parent.NestLevel + 1;
                        innerList.Insert(startIndex + ++i, child);
                    }
                    bulk.Dispose();
                    //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)parent.Children, startIndex + 1));
                }
            }
        }
        else
        {
            if (expandedParents.Remove(parent))
            {
                if (parent.Children.Count + parent.NestedParents.Count > 0)
                {
                    var bulk = innerList.SuspendNotifications();
                    foreach (var nestedParent in parent.NestedParents)
                    {
                        UninstallParent((P)nestedParent, startIndex + 1);
                    }
                    foreach (var child in parent.Children)
                    {
                        innerList.RemoveAt(startIndex + 1);
                    }
                    bulk.Dispose();
                    //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)parent.Children, startIndex + 1));
                }
            }
        }
    }

    public void Dispose()
    {
        foreach (var p in parents)
        {
            p.PropertyChanged -= OnParentPropChanged;
            p.ChildrenChanged -= OnChildrenChanged;
            p.NestedParentsChanged -= OnNestedParentsChanged;
        }
        parents.Clear();
    }

    private int CountVisibleItems(IParentType parent)
    {
        int count = 1;
        if (expandedParents.Contains(parent))
        {
            foreach (var nested in parent.NestedParents)
                count += CountVisibleItems(nested);

            count += parent.Children.Count;
        }

        return count;
    }

    private void OnNestedParentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        P parent = (P)sender!;
        SourceCollectionChanged?.Invoke(this, EventArgs.Empty);

        if (!expandedParents.Contains(parent))
            return;

        var startIndex = innerList.IndexOf(parent);
        int i = 0;

        var bulk = innerList.SuspendNotifications();
        if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (P nestedParent in e.OldItems!)
                UninstallParent(nestedParent, startIndex + 1);
        }
        else if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (P nestedParent in e.NewItems!)
                i += InstallParent(nestedParent, i + startIndex + 1, parent.NestLevel + 1);
        }
        else
            throw new Exception("not supported thing");

        bulk.Dispose();
        //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)parent.Children, startIndex + 1));                
    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        P parent = (P)sender!;
        SourceCollectionChanged?.Invoke(this, EventArgs.Empty);

        if (!expandedParents.Contains(parent))
            return;

        var startIndex = innerList.IndexOf(parent);
        int i = 0;

        foreach (var nested in parent.NestedParents)
            i += CountVisibleItems(nested);
        
        var bulk = innerList.SuspendNotifications();
        if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (C child in e.OldItems!)
            {
                innerList.RemoveAt(startIndex + 1 + e.OldStartingIndex + i);
                i++;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (C child in e.NewItems!)
            {
                child.NestLevel = parent.NestLevel + 1;
                innerList.Insert(startIndex + 1 + e.NewStartingIndex + i, child);
                i++;
            }
        }
        else
            throw new Exception("not supported thing");

        bulk.Dispose();
        //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)parent.Children, startIndex + 1));                
    }
    
    private IEnumerable<C> GetChildren(P parent)
    {
        foreach (var nested in parent.NestedParents)
        {
            foreach (var c in GetChildren((P)nested))
                yield return (C)c;
        }
        foreach (var child in parent.Children)
        {
            yield return (C)child;
        }
    }
    
    public IEnumerable<C> GetChildren()
    {
        foreach (var parent in roots)
        {
            foreach (var child in GetChildren(parent))
            {
                yield return child;
            }
        }
    }

    public IEnumerator<P> GetParentsEnumerator()
    {
        return parents.GetEnumerator();
    }
    
    public IEnumerable<P> GetRoots()
    {
        return roots;
    }
    
    IEnumerator<INodeType> IEnumerable<INodeType>.GetEnumerator()
    {
        return innerList.GetEnumerator();
    }

    public IEnumerator GetEnumerator()
    {
        return innerList.GetEnumerator();
    }

    public event EventHandler? SourceCollectionChanged;

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => innerList.CollectionChanged += value;
        remove => innerList.CollectionChanged -= value;
    }

    public void CopyTo(Array array, int index)
    {
        ((ICollection)innerList).CopyTo(array, index);
    }

    public int Count => innerList.Count;

    public bool IsSynchronized => ((ICollection)innerList).IsSynchronized;

    public object SyncRoot => ((ICollection)innerList).SyncRoot;

    public void Add(INodeType? value)
    {
        throw new Exception("not supported!");
        //return ((IList)innerList).Add(value);
    }

    public void Clear()
    {
        throw new Exception("not supported!");
    }

    public bool Contains(INodeType? value) => ((IList)innerList).Contains(value);
    
    public void CopyTo(INodeType[] array, int arrayIndex)
    {
        innerList.CopyTo(array, arrayIndex);
    }

    public int IndexOf(INodeType? value) => ((IList)innerList).IndexOf(value);

    public void Insert(int index, INodeType? value)
    {
        throw new Exception("not supported!");
    }

    public bool Remove(INodeType? value)
    {
        throw new Exception("not supported!");
    }

    public void RemoveAt(int index)
    {
        throw new Exception("not supported!");
    }

    public bool IsFixedSize => ((IList)innerList).IsFixedSize;

    public bool IsReadOnly => true;

    public INodeType this[int index]
    {
        get => innerList[index];
        set => throw new Exception("not supported!");
    }
}

public interface INodeType
{
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } // used internally for fast filtering
    public IParentType? Parent { get; set; }
    public bool CanBeExpanded { get; }
}

public interface IChildType : INodeType
{
}

public interface IParentType : INodeType, INotifyPropertyChanged
{
    public bool IsExpanded { get; set; }
    public IReadOnlyList<IParentType> NestedParents { get; }
    public IReadOnlyList<IChildType> Children { get; }
    event NotifyCollectionChangedEventHandler? ChildrenChanged;
    event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}