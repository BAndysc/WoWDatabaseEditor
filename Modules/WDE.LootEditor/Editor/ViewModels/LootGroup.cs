using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.LootEditor.Models;
using WDE.LootEditor.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.LootEditor.Editor.ViewModels;

public partial class LootGroup : ObservableBase, ITableRowGroup
{
    public LootEditorViewModel ParentVm { get; }

    [Notify] [AlsoNotify(nameof(IsModified))] private bool isGroupModified;

    [Notify] private bool isExpanded = true;
    
    [Notify] private bool isFocused;

    [Notify] private bool hasCrossReferencesCounter;
    
    [Notify] private int crossReferencesCount;

    [Notify] private bool dontLoadRecursively;
    
    private string? groupName;

    public string? GroupName
    {
        get => groupName;
        set
        {
            if (groupName == value)
                return;
            
            groupName = value;
            IsGroupModified = true;
            ParentVm.InvalidateReferenceLootNames();
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(Header));
        }
    }
    
    public event Func<LootGroup, EventArgs, Task>? OnReferenceLootChanged;
    
    public event Func<LootItemViewModel, OnBeforeChangedEventArgs<long>, Task>? OnBeforeReferenceLootChanged;
    
    public ObservableCollection<LootItemViewModel> LootItems { get; } = new();
    
    public LootSourceType LootSourceType { get; }

    public bool CanBeUnloaded => (ParentVm.LootEditingMode == LootEditingMode.PerDatabaseTable && !IsDynamicallyLoadedReference) ||
                                 ParentVm.LootEditingMode == LootEditingMode.PerLogicalEntity && !ParentVm.PerEntityLootSolutionItem!.SolutionEntryIsLootEntry() && ParentVm.PerEntityLootSolutionItem!.Type.CanUpdateSourceLootEntry() && LootSourceType != LootSourceType.Reference;

    public bool IsDynamicallyLoadedReference => LootSourceType == LootSourceType.Reference &&
                                                ((ParentVm.PerEntityLootSolutionItem != null && (uint)LootEntry != ParentVm.PerEntityLootSolutionItem.Entry) ||
                                                (ParentVm.PerEntityLootSolutionItem == null && !ParentVm.PerDatabaseSolutionItems.Contains(LootEntry)));
    
    public LootEntry LootEntry { get; }
    public uint? SourceEntityEntry { get; }

    public IReadOnlyList<ITableRow> Rows => LootItems;

    public string Header => (LootSourceType == LootSourceType.Reference ? "Reference" : "Loot id") + " " + (uint)LootEntry + (groupName == null ? "" : $": {groupName}");

    public event Action<ITableRowGroup, ITableRow>? RowChanged;
    
    public event Action<ITableRowGroup>? RowsChanged;
    
    public ICommand AddNewLootItemCommand { get; }

    public bool IsModified => LootItems.Any(x => x.IsModified) || IsGroupModified;

    public bool CanHaveName { get; } 
    
    private LootGroup(LootEditorViewModel parentVM)
    {
        ParentVm = parentVM;
        LootItems.CollectionChanged += OptionsOnCollectionChanged;
        AddNewLootItemCommand = new DelegateCommand(() =>
        {
            var vm = new LootItemViewModel(this);
            LootItems.Add(vm);
        });
        this.ToObservable(x => x.IsExpanded)
            .SubscribeAction(_ => RowsChanged?.Invoke(this));
        this.ToObservable(x => x.GroupName)
            .SubscribeAction(_ => IsGroupModified = true);
    }
    
    public LootItemViewModel AddNewLootItem(long itemId)
    {
        var vm = new LootItemViewModel(this);
        vm.ItemOrCurrencyId.SetValue(itemId).ListenErrors();
        LootItems.Add(vm);
        return vm;
    }

    public void AddNewLootItem(AbstractLootEntry loot)
    {
        var vm = new LootItemViewModel(this, new LootModel(loot));
        LootItems.Add(vm);
    }
    
    public LootGroup(LootEditorViewModel parentVM, LootSourceType type, LootEntry lootEntry, uint? sourceEntityEntry, ILootTemplateName? name) : this(parentVM)
    {
        LootSourceType = type;
        LootEntry = lootEntry;
        SourceEntityEntry = sourceEntityEntry;
        CanHaveName = parentVM.LootEditorFeatures.LootGroupHasName(type);
        groupName = name?.Name;
        dontLoadRecursively = name?.DontLoadRecursively ?? false;
        CalculateCrossReferences().ListenErrors();
    }

    private async Task CalculateCrossReferences()
    {
        if ((uint)LootEntry == 0)
            return;
        
        switch (LootSourceType)
        {
            case LootSourceType.Creature:
            {
                var crossRefs = await ParentVm.DatabaseProvider.GetCreatureLootCrossReference((uint)LootEntry);
                CrossReferencesCount = crossRefs.Item1.Count + crossRefs.Item2.Count;
                HasCrossReferencesCounter = true;
                break;
            }
            case LootSourceType.GameObject:
            {
                var crossRefs = await ParentVm.DatabaseProvider.GetGameObjectLootCrossReference((uint)LootEntry);
                CrossReferencesCount = crossRefs.Count;
                HasCrossReferencesCounter = true;
                break;
            }
            case LootSourceType.Skinning:
            {
                var crossRefs = await ParentVm.DatabaseProvider.GetCreatureSkinningLootCrossReference((uint)LootEntry);
                CrossReferencesCount = crossRefs.Item1.Count + crossRefs.Item2.Count;
                HasCrossReferencesCounter = true;
                break;
            }
            case LootSourceType.Reference:
            {
                var crossRefs = await ParentVm.DatabaseProvider.GetReferenceLootCrossReference((uint)LootEntry);
                CrossReferencesCount = crossRefs.Count;
                HasCrossReferencesCounter = true;
                break;
            }
            case LootSourceType.Pickpocketing:
            {
                var crossRefs = await ParentVm.DatabaseProvider.GetCreaturePickPocketLootCrossReference((uint)LootEntry);
                CrossReferencesCount = crossRefs.Item1.Count + crossRefs.Item2.Count;
                HasCrossReferencesCounter = true;
                break;
            }
        }
    }
    
    private void OptionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset) 
            throw new Exception("Reset not supported, because it's not possible to determine which rows changed");
        
        if (e.OldItems != null)
        {
            var groupIndex = ParentVm.Loots.IndexOf(this);
            int i = 0;
            
            for (int j = 0; j < e.OldItems.Count; ++j)
                ParentVm.MultiSelection.Remove(new VerticalCursor(groupIndex, j + e.OldStartingIndex));
            
            for (int j = e.OldStartingIndex + e.OldItems.Count; j < LootItems.Count; ++j)
            {
                var oldCursor = new VerticalCursor(groupIndex, j);
                var newCursor = new VerticalCursor(groupIndex, j - e.OldItems.Count);
                if (ParentVm.MultiSelection.Contains(oldCursor))
                {
                    ParentVm.MultiSelection.Remove(oldCursor);
                    ParentVm.MultiSelection.Add(newCursor);
                }
            }
            
            foreach (LootItemViewModel item in e.OldItems)
            {
                int index = e.OldStartingIndex + i;
                item.OnReferenceLootChanged -= ItemOnOnSubMenuChanged;
                item.OnBeforeReferenceLootChanged -= ItemsOnBeforeSubMenuChanged;
                item.PropertyChanged -= ItemPropertyChanged;
                item.PropertyValueChanged -= ItemPropertyValueChanged;
                item.OnHistoryAction -= ItemOnHistoryAction;
                item.Changed -= ItemChanged;
                
                ParentVm.HistoryHandler.PushAction(new AnonymousHistoryAction("Loot item removed", () =>
                {
                    LootItems.Insert(index, item);
                }, () =>
                {
                    LootItems.Remove(item);
                }));
                i++;
            }
        }

        if (e.NewItems != null)
        {
            int i = 0;
            foreach (LootItemViewModel item in e.NewItems)
            {
                int index = e.NewStartingIndex + i;
                item.OnReferenceLootChanged += ItemOnOnSubMenuChanged;
                item.OnBeforeReferenceLootChanged += ItemsOnBeforeSubMenuChanged;
                item.PropertyChanged += ItemPropertyChanged;
                item.PropertyValueChanged += ItemPropertyValueChanged;
                item.OnHistoryAction += ItemOnHistoryAction;
                item.Changed += ItemChanged;
                
                ParentVm.HistoryHandler.PushAction(new AnonymousHistoryAction("Loot item added", () =>
                {
                    LootItems.Remove(item);
                }, () =>
                {
                    LootItems.Insert(index, item);
                }));
                i++;
            }
        }
        
        RowsChanged?.Invoke(this);
        if (!ParentVm.History.IsUndoing && !ParentVm.IsLoading)
            IsGroupModified = true;
    }

    private void ItemChanged(ITableRow obj)
    {
        RowChanged?.Invoke(this, obj);
    }

    private void ItemOnHistoryAction(IHistoryAction obj)
    {
        ParentVm.HistoryHandler.PushAction(obj);
    }

    private void ItemPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
    {
        var prop = sender.GetType().GetProperty(e.PropertyName);
        ParentVm.HistoryHandler.PushAction(new AnonymousHistoryAction("Set " + e.PropertyName, () =>
        {
            prop!.SetValue(sender, e.Old);
        }, () =>
        {
            prop!.SetValue(sender, e.New);
        }));
    }

    private void ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RowChanged?.Invoke(this, (ITableRow)sender!);
    }

    private async Task ItemOnOnSubMenuChanged(LootItemViewModel option, EventArgs _)
    {
        await OnReferenceLootChanged.Raise(option.Parent, EventArgs.Empty);
    }

    private Task ItemsOnBeforeSubMenuChanged(LootItemViewModel sender, OnBeforeChangedEventArgs<long> e)
    {
        return OnBeforeReferenceLootChanged.Raise(sender, e);
    }

    public void SetAsSaved()
    {
        foreach (var option in LootItems)
            option.SetAsSaved();
        IsGroupModified = false;
    }

    public void RaisePropertyChangedPublic(string name) => RaisePropertyChanged(name);
}





























