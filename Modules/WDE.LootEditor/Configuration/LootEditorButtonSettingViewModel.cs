using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.LootEditor.Services;
using WDE.MVVM;

namespace WDE.LootEditor.Configuration;

public partial class LootEditorButtonSettingViewModel : ObservableBase, ITableRowGroup, IReadOnlyList<ITableRowGroup>
{
    public LootEditorConfiguration Parent { get; }
    [Notify] [AlsoNotify(nameof(IsNonNullIcon))] private IconViewModel? icon;
    [Notify] [AlsoNotify(nameof(ButtonTextOrToolTip))] private string? buttonText;
    [Notify] [AlsoNotify(nameof(ButtonTextOrToolTip))] private string? buttonToolTip;
    [Notify] [AlsoNotify(nameof(IsCustomItemSetType))] private LootButtonType type;
    
    public bool IsNonNullIcon
    {
        get => icon != null;
        set
        {
            if (!value)
                icon = null;
            else if (icon == null)
                icon = StaticIcons.Icons.FirstOrDefault();
            RaisePropertyChanged(nameof(Icon));
            RaisePropertyChanged(nameof(IsNonNullIcon));
        }
    }
    
    public bool IsCustomItemSetType => type == LootButtonType.AddItemSet;
    
    [AlsoNotify(nameof(FocusedRow))] 
    [Notify] private VerticalCursor focusedRowIndex = VerticalCursor.None;
        
    [Notify] private int focusedCellIndex = -1;
    
    public ITableMultiSelection MultiSelection { get; } = new TableMultiSelection();
    
    public CustomLootItemSettingViewModel? FocusedRow =>
        focusedRowIndex.RowIndex >= 0 && focusedRowIndex.RowIndex < Rows.Count ?
            customItems[focusedRowIndex.RowIndex] : null;
    
    public DelegateCommand AddNewItemCommand { get; }
    
    public DelegateCommand DeleteSelectedLootItemsCommand { get; }
    
    public AsyncAutoCommand PickCustomIconCommand { get; }
    
    public LootEditorButtonSettingViewModel(LootEditorConfiguration parent,
        LootButtonDefinition definition)
    {
        Parent = parent;
        icon = definition.Icon == null ? null : new IconViewModel(new ImageUri(definition.Icon));
        buttonText = definition.ButtonText;
        buttonToolTip = definition.ButtonToolTip;
        type = definition.ButtonType;
        customItems.CollectionChanged += OnCustomItemChanged;
        if (definition.CustomItems != null)
        {
            foreach (var customItemDef in definition.CustomItems)
            {
                customItems.Add(new CustomLootItemSettingViewModel(this, customItemDef));
            }
        }

        DeleteSelectedLootItemsCommand = new DelegateCommand(() =>
        {
            foreach (var cursor in MultiSelection.AllReversed())
            {
                customItems.RemoveAt(cursor.RowIndex);
            }
            MultiSelection.Clear();
        });
        
        AddNewItemCommand = new DelegateCommand(() =>
        {
            customItems.Add(new CustomLootItemSettingViewModel(this));
        });

        PickCustomIconCommand = new AsyncAutoCommand(async () =>
        {
            var path = await parent.WindowManager.ShowOpenFileDialog("Images|png,jpg,jpeg,bmp,gif|All files|*", parent.FileSystem.ResolvePhysicalPath("/common/item_icons/").FullName);
            if (path != null)
            {
                Icon = new IconViewModel(new ImageUri(path));
            }
        });
    }

    private void OnCustomItemChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (CustomLootItemSettingViewModel @new in e.NewItems)
            {
                @new.Changed += OnItemChanged;
            }
        
        if (e.OldItems != null)
            foreach (CustomLootItemSettingViewModel old in e.OldItems)
            {
                old.Changed -= OnItemChanged;
            }
        
        RowsChanged?.Invoke(this);
    }

    private void OnItemChanged(ITableRow obj)
    {
        RowChanged?.Invoke(this, obj);
    }

    public string? ButtonTextOrToolTip => buttonText ?? buttonToolTip;

    public LootButtonDefinition ToDefinition()
    {
        return new LootButtonDefinition()
        {
            Icon = icon?.Path,
            ButtonText = buttonText,
            ButtonToolTip = buttonToolTip,
            ButtonType = type,
            CustomItems = customItems.Select(x => x.ToDefinition())
                .ToList()
        };
    }

    private ObservableCollection<CustomLootItemSettingViewModel> customItems = new();
    public IReadOnlyList<ITableRow> Rows => customItems;
    public event Action<ITableRowGroup, ITableRow>? RowChanged;
    public event Action<ITableRowGroup>? RowsChanged;
    
    public IEnumerator<ITableRowGroup> GetEnumerator()
    {
        yield return this;
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => 1;
    public ITableRowGroup this[int index] => this;
}