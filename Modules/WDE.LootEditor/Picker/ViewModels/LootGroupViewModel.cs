using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.LootEditor.Picker.ViewModels;

public class LootGroupViewModel : ObservableBase, ITableRowGroup
{
    public LootPickerViewModel Parent { get; }
    public uint LootEntry { get; }
    public string? Name { get; }
    public List<ItemViewModel> AllItemsRaw { get; } = new();
    public List<ItemViewModel> AllItemsFlatten { get; } = new();
    public ObservableCollection<ItemViewModel> FilteredItems { get; } = new();
    public IReadOnlyList<ItemViewModel> AllItems => Parent.FlattenReferences ? AllItemsFlatten : AllItemsRaw;
    
    public int VisibleItemsCount => FilteredItems.Count;

    public int AllItemsCount => AllItems.Count;

    public bool HasHiddenItems => VisibleItemsCount != AllItemsCount;

    public string Header => Name == null ? LootEntry.ToString() : $"{Name} ({LootEntry})";
    
    public LootGroupViewModel(LootPickerViewModel parent,
        uint lootEntry,
        string? name)
    {
        Parent = parent;
        LootEntry = lootEntry;
        Name = name;
    }
    
    public void PerformSearch(string search)
    {
        FilteredItems.RemoveAll();
        if (string.IsNullOrWhiteSpace(search))
            FilteredItems.AddRange(AllItems);
        else
        {
            foreach (var item in AllItems)
            {
                if (item.Search(search))
                    FilteredItems.Add(item);
            }
        }
        RowsChanged?.Invoke(this);
        RaisePropertyChanged(nameof(VisibleItemsCount));
        RaisePropertyChanged(nameof(HasHiddenItems));
    }

    public IReadOnlyList<ITableRow> Rows => FilteredItems;
    public event Action<ITableRowGroup, ITableRow>? RowChanged;
    public event Action<ITableRowGroup>? RowsChanged;
}