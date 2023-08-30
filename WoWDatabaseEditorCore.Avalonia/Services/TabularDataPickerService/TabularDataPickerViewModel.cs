using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
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
    private readonly ITabularDataPickerPreferences preferences;
    private readonly ITabularDataArgs<object> args;

    [Notify] 
    private string searchText = "";

    public IMultiIndexContainer Selection { get; } = new MultiIndexContainer();
    
    public IMultiIndexContainer CheckedIndices { get; } = new MultiIndexContainer();

    [Notify]
    [AlsoNotify(nameof(FocusedItem))]
    private int focusedIndex = -1;

    private bool ignoreCheckedIndicesChange = false;
    private bool ignoreSelectionChange = false;
    private HashSet<object> selectedItems = new HashSet<object>();
    private HashSet<object> checkedItems = new HashSet<object>();

    public IReadOnlyCollection<object> SelectedItems => selectedItems;
    
    public IReadOnlyCollection<object> CheckedItems => checkedItems;

    private IIndexedCollection<object> allItems;
    private readonly Func<object, string, bool> filterPredicate;
    private readonly Func<object, long, bool>? numberPredicate;
    private readonly Func<object,string,bool>? exactMatchPredicate;
    private readonly Func<string, object?>? exactMatchCreator;
    public IIndexedCollection<object> Items { get; private set; }
    public IReadOnlyList<ColumnDescriptor> Columns { get; }
    
    public object? FocusedItem => focusedIndex >= 0 && focusedIndex < Items.Count ? Items[focusedIndex] : null;

    public bool MultiSelect { get; }
    
    public bool CheckBoxes { get; }
    
    public TabularDataPickerViewModel(ITabularDataPickerPreferences preferences,
        ITabularDataArgs<object> args, 
        bool multiSelection, 
        bool checkBoxes,
        string defaultSearchText,
        IReadOnlyList<int>? defaultSelection = null)
    {
        this.preferences = preferences;
        this.args = args;
        Title = args.Title;
        MultiSelect = multiSelection;
        CheckBoxes = checkBoxes;
        var savedWidths = preferences.GetSavedColumnsWidth(Title);
        Columns = args.Columns
            .Select((c, index) =>
            {
                var width = c.Width;
                if (savedWidths?.TryGetValue(c.Header, out var size) ?? false)
                    width = size;
                if (c.DataTemplate is IDataTemplate)
                    return ColumnDescriptor.DataTemplateColumn(c.Header, c.DataTemplate, width, false);
                else if (c.DataTemplate is Func<object, string> f)
                    return ColumnDescriptor.DataTemplateColumn(c.Header, new FuncDataTemplate(_ => true, (_, _) => new TextBlock()
                    {
                        [!TextBlock.TextProperty] = new Binding(c.PropertyName){Converter = new FuncValueConverter<object, string>(o => f(o!))}
                    }), width, false);
                else if (c is ITabularDataAsyncColumn asyncColumn)
                {
                    return ColumnDescriptor.DataTemplateColumn(c.Header, new FuncDataTemplate(_ => true, (_, _) => new AsyncDynamicTextBlock()
                    {
                        [!AsyncDynamicTextBlock.ValueProperty] = new Binding(c.PropertyName),
                        Evaluator = asyncColumn.ComputeAsync
                    }), width, false);
                }
                return ColumnDescriptor.TextColumn(c.Header, c.PropertyName, width, false);
            })
            .ToList();
        allItems = args.Data;
        filterPredicate = args.FilterPredicate;
        numberPredicate = args.NumberPredicate;
        exactMatchPredicate = args.ExactMatchPredicate;
        exactMatchCreator = args.ExactMatchCreator;
        Items = args.Data;
        
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
        if (checkBoxes)
        {
            CheckedIndices.Cleared += () =>
            {
                if (ignoreCheckedIndicesChange)
                    return;
                checkedItems.Clear();
            };
            CheckedIndices.Added += i =>
            {
                if (ignoreCheckedIndicesChange)
                    return;
                checkedItems.Add(Items[i]);
            };
            CheckedIndices.Removed += i =>
            {
                if (ignoreCheckedIndicesChange)
                    return;
                checkedItems.Remove(Items[i]);
            };
        }
        
        if (defaultSelection != null && defaultSelection.Count > 0)
        {
            focusedIndex = defaultSelection[0];
            foreach (var index in defaultSelection)
            {
                if (index >= 0 && index < Items.Count)
                {
                    (checkBoxes ? CheckedIndices : Selection).Add(index);
                }   
            }
        }
        
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

        if (this.preferences.GetWindowState(args.Title, out _, out _, out _, out int width, out int height))
        {
            DesiredWidth = width;
            DesiredHeight = height;
        }
        else
        {
            DesiredWidth = 700;
            DesiredHeight = 750;
        }
        
        searchText = defaultSearchText;
        Filter(defaultSearchText);
        this.ToObservable<string, TabularDataPickerViewModel>(() => SearchText)
            .Throttle(TimeSpan.FromMilliseconds(50), AvaloniaScheduler.Instance)
            .Subscribe(Filter);
        On(() => Items, _ => acceptCommand.RaiseCanExecuteChanged());
    }

    private void UpdateCheckBoxAndSelectionAfterFilter()
    {
        if (selectedItems.Count > 0)
        {
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

        if (checkedItems.Count > 0)
        {
            try
            {
                ignoreCheckedIndicesChange = true;
                CheckedIndices.Clear();
                for (int i = 0, count = Items.Count; i < count; ++i)
                {
                    if (checkedItems.Contains(Items[i]))
                        CheckedIndices.Add(i);
                }
            }
            finally
            {
                ignoreCheckedIndicesChange = false;
            }
        }
    }
    
    private void Filter(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Items = allItems;
            RaisePropertyChanged(nameof(Items));
            UpdateCheckBoxAndSelectionAfterFilter();
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
        UpdateCheckBoxAndSelectionAfterFilter();
    }
    
    public void SaveColumnsWidth(List<int> widths)
    {
        if (widths.Count != args.Columns.Count)
        {
            Console.WriteLine("When trying to save columns width, the number of columns is different from the number of widths!!! So can't save. Report to the developer.");
            return;
        }
        
        List<(string, int)> widthsToSave = new(); 
        for (int i = 0, count = args.Columns.Count; i < count; ++i)
        {
            var key = args.Columns[i].Header;
            var width = widths[i];
            if (Math.Abs(width - (int)(args.Columns[i].Width)) < 5)
                continue;
            if (width < 1)
                continue;
            widthsToSave.Add((key, width));
        }
        preferences.UpdateColumnsWidth(Title, widthsToSave);
    }
    
    public int DesiredWidth { get; }
    public int DesiredHeight { get; }
    public string Title { get; }
    public bool Resizeable => true;
    private DelegateCommand acceptCommand;
    public ICommand Accept => acceptCommand;
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}

