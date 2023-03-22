using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Threading;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Collections;
using WDE.Common.Managers;
using WDE.Common.TableData;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Avalonia.Services.TabularDataPickerService;

public partial class TabularDataPickerViewModel : ObservableBase, IDialog
{
    [Notify] 
    private string searchText = "";

    public IMultiIndexContainer Selection { get; } = new MultiIndexContainer();

    [Notify]
    [AlsoNotify(nameof(FocusedItem))]
    private int focusedIndex = -1;

    private bool ignoreSelectionChange = false;
    private HashSet<object> selectedItems = new HashSet<object>();

    public IReadOnlyCollection<object> SelectedItems => selectedItems;

    private IIndexedCollection<object> allItems;
    private readonly Func<object, string, bool> filterPredicate;
    private readonly Func<object, long, bool>? numberPredicate;
    private readonly Func<object,string,bool>? exactMatchPredicate;
    private readonly Func<string, object?>? exactMatchCreator;
    public IIndexedCollection<object> Items { get; private set; }
    public IReadOnlyList<ColumnDescriptor> Columns { get; }
    
    public object? FocusedItem => focusedIndex >= 0 && focusedIndex < Items.Count ? Items[focusedIndex] : null;

    public bool MultiSelect { get; }
    
    public TabularDataPickerViewModel(ITabularDataArgs<object> args, bool multiSelection, IReadOnlyList<int>? defaultSelection = null)
    {
        Title = args.Title;
        MultiSelect = multiSelection;
        Columns = args.Columns
            .Select((c, index) =>
            {
                if (c.DataTemplate is { })
                    return ColumnDescriptor.DataTemplateColumn(c.Header, c.DataTemplate, c.Width, false);
                else if (c is ITabularDataAsyncColumn asyncColumn)
                {
                    return ColumnDescriptor.DataTemplateColumn(c.Header, new FuncDataTemplate(_ => true, (_, _) => new AsyncDynamicTextBlock()
                    {
                        [!AsyncDynamicTextBlock.ValueProperty] = new Binding(c.PropertyName),
                        Evaluator = asyncColumn.ComputeAsync
                    }), c.Width, false);
                }
                return ColumnDescriptor.TextColumn(c.Header, c.PropertyName, c.Width, false);
            })
            .ToList();
        allItems = args.Data;
        filterPredicate = args.FilterPredicate;
        numberPredicate = args.NumberPredicate;
        exactMatchPredicate = args.ExactMatchPredicate;
        exactMatchCreator = args.ExactMatchCreator;
        Items = args.Data;
        if (defaultSelection != null && defaultSelection.Count > 0)
        {
            focusedIndex = defaultSelection[0];
            foreach (var index in defaultSelection)
            {
                if (index >= 0 && index < Items.Count)
                    Selection.Add(index);   
            }
        }
        
        Selection.Cleared += () =>
        {
            if (ignoreSelectionChange)
                return;
            selectedItems.Clear();
        };
        Selection.Added += i =>
        {
            if (ignoreSelectionChange)
                return;
            selectedItems.Add(Items[i]);
        };
        Selection.Removed += i =>
        {
            if (ignoreSelectionChange)
                return;
            selectedItems.Remove(Items[i]);
        };
        
        acceptCommand = new DelegateCommand(() =>
        {
            if (FocusedItem == null && Items.Count > 0)
            {
                FocusedIndex = 0;
                Selection.Add(FocusedIndex);
            }
            CloseOk?.Invoke();
        }, () => FocusedItem != null || Items.Count > 0).ObservesProperty(() => FocusedItem);
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
        
        this.ToObservable<string, TabularDataPickerViewModel>(() => SearchText)
            .Throttle(TimeSpan.FromMilliseconds(50), AvaloniaScheduler.Instance)
            .Subscribe(Filter);
        On(() => Items, _ => acceptCommand.RaiseCanExecuteChanged());
    }

    private void UpdateSelectionAfterFilter()
    {
        if (selectedItems.Count == 0)
            return;
        
        try
        {
            ignoreSelectionChange = true;
            Selection.Clear();
            FocusedIndex = -1;
            for (int i = 0, count = Items.Count; i < count; ++i)
            {
                if (selectedItems.Contains(Items[i]))
                {
                    Selection.Add(i);
                    FocusedIndex = i;
                }
            }
        }
        finally
        {
            ignoreSelectionChange = false;
        }
    }
    
    private void Filter(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Items = allItems;
            RaisePropertyChanged(nameof(Items));
            UpdateSelectionAfterFilter();
            return;
        }
        
        List<object> filtered = new();
        text = text.Trim();
        var numberFilter = long.TryParse(text, out var number);
        bool hasExactMatch = false;
        if (numberFilter && numberPredicate != null)
        {
            for (int i = 0, count = allItems.Count; i < count; i++)
            {
                object item = allItems[i];
                var accept = numberPredicate(item, number);
                if (accept)
                    filtered.Add(item);
                if (!hasExactMatch && exactMatchPredicate != null)
                    hasExactMatch = exactMatchPredicate(item, text);
            }
        }
        else
        {
            for (int i = 0, count = allItems.Count; i < count; i++)
            {
                object item = allItems[i];
                var accept = filterPredicate(item, text);
                if (accept)
                    filtered.Add(item);
                if (!hasExactMatch && exactMatchPredicate != null)
                    hasExactMatch = exactMatchPredicate(item, text);
            }
        }

        if (!hasExactMatch && exactMatchCreator != null)
        {
            var dynamic = exactMatchCreator(text);
            if (dynamic != null)
                filtered.Add(dynamic);
        }

        Items = filtered.AsIndexedCollection();
        RaisePropertyChanged(nameof(Items));
        UpdateSelectionAfterFilter();
    }

    public int DesiredWidth => 700;
    public int DesiredHeight => 750;
    public string Title { get; }
    public bool Resizeable => true;
    private DelegateCommand acceptCommand;
    public ICommand Accept => acceptCommand;
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}

