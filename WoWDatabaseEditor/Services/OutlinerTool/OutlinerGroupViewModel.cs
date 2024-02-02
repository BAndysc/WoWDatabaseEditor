using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PropertyChanged.SourceGenerator;
using WDE.Common.Utils;

namespace WoWDatabaseEditorCore.Services.OutlinerTool;

public partial class OutlinerGroupViewModel : IParentType
{
    [Notify] private bool isExpanded = true;
    private string name;
    public string Name { get; private set; }

    private bool isRefreshing;
    public bool IsRefreshing
    {
        get => isRefreshing;
        set
        {
            if (isRefreshing == value)
                return;
            isRefreshing = value;
            if (value)
                Name = name + " (refreshing...)";
            else
                Name = name;
        }
    }
    
    public ObservableCollection<OutlinerItemViewModel> Items { get; } = new();

    public OutlinerGroupViewModel(string name)
    {
        this.name = name;
        Name = name;
        Items.CollectionChanged += ItemsOnCollectionChanged;
    }

    private void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ChildrenChanged?.Invoke(this, e);
    }

    public void Clear()
    {
        Items.RemoveAll();
    }

    public void Add(OutlinerItemViewModel item)
    {
        Items.Add(item);
    }

    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }
    public IReadOnlyList<IParentType> NestedParents => Array.Empty<IParentType>();
    public IReadOnlyList<IChildType> Children => Items;
    public bool CanBeExpanded => true;

    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}