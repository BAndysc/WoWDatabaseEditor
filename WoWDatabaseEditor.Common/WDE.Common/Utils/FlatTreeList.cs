using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using DynamicData.Binding;

namespace WDE.Common.Utils;

public class FlatTreeList<P, C> : IDisposable, IEnumerable, IEnumerable<object>, IList, INotifyCollectionChanged where P : IParentType where C : IChildType
{
    private ObservableCollectionExtended<object> innerList = new();
    private List<P> parents = new();
    
    public FlatTreeList(IReadOnlyList<P> parents)
    {
        InstallParents(parents);
        if (parents is INotifyCollectionChanged incc)
            incc.CollectionChanged += OnParentCollectionChanged;
    }

    private void InstallParents(IReadOnlyList<P> parents)
    {
        foreach (var parent in parents)
        {
            InstallParent(parent); 
        }
    }

    private void InstallParent(P parent)
    {
        innerList.Add(parent);
        if (parent.IsExpanded)
        {
            foreach (var child in parent.Children)
            {
                innerList.Add(child);
            }
        }
        this.parents.Add(parent);
        parent.PropertyChanged += OnParentPropChanged;
        parent.CollectionChanged += OnParentInternalCollectionChanged;
    }

    private void OnParentCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var parent in parents)
            {
                parent.PropertyChanged -= OnParentPropChanged;
                parent.CollectionChanged -= OnParentInternalCollectionChanged;
            }
            parents.Clear();
            innerList.Clear();
            InstallParents((sender as IReadOnlyList<P>)!);
        }
        else if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems is IReadOnlyList<P> pList)
                InstallParents(pList);
            else
                foreach (P p in e.NewItems!)
                    InstallParent(p);
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            throw new Exception("not supported thing");
            foreach (P oldParent in e.OldItems!)
            {
                
            }
        }
    }

    private void OnParentPropChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IParentType.IsExpanded))
            return;
        
        P parent = (P)sender!;
        var startIndex = innerList.IndexOf(parent);
        if (parent.IsExpanded)
        {
            int i = 0;

            if (parent.Children.Count > 0)
            {
                var bulk = innerList.SuspendNotifications();
                foreach (var child in parent.Children)
                {
                    innerList.Insert(startIndex + ++i, child);
                }
                bulk.Dispose();
                //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)parent.Children, startIndex + 1));                
            }
        }
        else
        {
            if (parent.Children.Count > 0)
            {
                var bulk = innerList.SuspendNotifications();
                foreach (var child in parent.Children)
                {
                    innerList.RemoveAt(startIndex + 1);
                }
                bulk.Dispose();
                //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)parent.Children, startIndex + 1));                
            }
        }
    }

    public void Dispose()
    {
        foreach (var p in parents)
        {
            p.PropertyChanged -= OnParentPropChanged;
            p.CollectionChanged -= OnParentInternalCollectionChanged;
        }
        parents.Clear();
    }

    private void OnParentInternalCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        P parent = (P)sender!;
        if (!parent.IsExpanded)
            return;

        var startIndex = innerList.IndexOf(parent);
        int i = 0;

        if (parent.Children.Count > 0)
        {
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
                    innerList.Insert(startIndex + 1 + e.NewStartingIndex + i, child);
                    i++;
                }
            }
            else
                throw new Exception("not supported thing");

            bulk.Dispose();
            //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)parent.Children, startIndex + 1));                
        }
    }

    public IEnumerator<C> GetChildrenEnumerator()
    {
        foreach (var parent in parents)
        {
            foreach (var child in parent.Children)
            {
                yield return (C)child;
            }
        }
    }

    public IEnumerable<C> GetChildren()
    {
        foreach (var parent in parents)
        {
            foreach (var child in parent.Children)
            {
                yield return (C)child;
            }
        }
    }

    public IEnumerator<P> GetParentsEnumerator()
    {
        return parents.GetEnumerator();
    }
    
    IEnumerator<object> IEnumerable<object>.GetEnumerator()
    {
        return innerList.GetEnumerator();
    }

    public IEnumerator GetEnumerator()
    {
        return innerList.GetEnumerator();
    }

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

    public int Add(object? value)
    {
        throw new Exception("not supported!");
        return ((IList)innerList).Add(value);
    }

    public void Clear()
    {
        throw new Exception("not supported!");
        innerList.Clear();
    }

    public bool Contains(object? value) => ((IList)innerList).Contains(value);

    public int IndexOf(object? value) => ((IList)innerList).IndexOf(value);

    public void Insert(int index, object? value)
    {
        throw new Exception("not supported!");
        ((IList)innerList).Insert(index, value);
    }

    public void Remove(object? value)
    {
        throw new Exception("not supported!");
        ((IList)innerList).Remove(value);
    }

    public void RemoveAt(int index)
    {
        throw new Exception("not supported!");
        innerList.RemoveAt(index);
    }

    public bool IsFixedSize => ((IList)innerList).IsFixedSize;

    public bool IsReadOnly => true;

    public object? this[int index]
    {
        get => ((IList)innerList)[index];
        set => throw new Exception("not supported!");
    }
}

public interface IChildType
{
    
}

public interface IParentType : INotifyPropertyChanged, INotifyCollectionChanged
{
    public bool IsExpanded { get; set; }
    public IReadOnlyList<IChildType> Children { get; }
}