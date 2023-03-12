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
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Avalonia.Services.TabularDataPickerService;

public partial class TabularDataPickerViewModel : ObservableBase, IDialog
{
    [Notify] 
    private string searchText = "";
    
    [Notify] 
    [AlsoNotify(nameof(SelectedItem))] 
    private int selectedIndex = -1;
    
    private IIndexedCollection<object> allItems;
    private readonly Func<object, string, bool> filterPredicate;
    private readonly Func<object, long, bool>? numberPredicate;
    public IIndexedCollection<object> Items { get; private set; }
    public IReadOnlyList<ColumnDescriptor> Columns { get; }
    
    public object? SelectedItem => selectedIndex >= 0 && selectedIndex < Items.Count ? Items[selectedIndex] : null;

    public TabularDataPickerViewModel(ITabularDataArgs<object> args, int defaultSelection = -1)
    {
        Title = args.Title;
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
        Items = args.Data;
        selectedIndex = defaultSelection;
        
        acceptCommand = new DelegateCommand(() =>
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedIndex = 0;
            CloseOk?.Invoke();
        }, () => SelectedItem != null || Items.Count > 0).ObservesProperty(() => SelectedItem);
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
        
        this.ToObservable<string, TabularDataPickerViewModel>(() => SearchText)
            .Throttle(TimeSpan.FromMilliseconds(50), AvaloniaScheduler.Instance)
            .Subscribe(Filter);
        On(() => Items, _ => acceptCommand.RaiseCanExecuteChanged());
    }

    private void Filter(string text)
    {
        var selectedItem = SelectedItem;
        if (string.IsNullOrWhiteSpace(text))
        {
            Items = allItems;
            RaisePropertyChanged(nameof(Items));
            if (selectedItem != null)
            {
                SelectedIndex = -1;
                for (int i = 0; i < allItems.Count; ++i)
                {
                    if (allItems[i] == selectedItem)
                    {
                        SelectedIndex = i;
                        break;
                    }
                }
            }
            return;
        }
        
        List<object> filtered = new();
        text = text.Trim();
        var numberFilter = long.TryParse(text, out var number);
        if (numberFilter && numberPredicate != null)
        {
            for (int i = 0, count = allItems.Count; i < count; i++)
            {
                object item = allItems[i];
                var accept = numberPredicate(item, number);
                if (accept)
                    filtered.Add(item);
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
            }
        }

        Items = filtered.AsIndexedCollection();
        RaisePropertyChanged(nameof(Items));
        
        SelectedIndex = -1;
        if (selectedItem != null)
        {
            for (int i = 0, count = filtered.Count; i < count; ++i)
            {
                if (filtered[i] == selectedItem)
                {
                    SelectedIndex = i;
                    break;
                }
            }
        }
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

