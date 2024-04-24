using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using WDE.MVVM.Observable;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Views;

public partial class QuestPickerView : UserControl
{
    public QuestPickerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        searchBox = this.GetControl<TextBox>("SearchBox");
        resultsList = this.GetControl<ListBox>("ResultsList");
        adapter = new SelectingItemsControlSelectionAdapter(resultsList);
        adapter.Commit += ResultCommit;
        searchBox.GotFocus += (_, _) =>
        {
            if (searchBoxMoveToEnd)
                searchBox.SelectionEnd = searchBox.SelectionStart = searchBox.Text?.Length ?? 0;
            else
                searchBox.SelectAll();
        };
        searchBox.KeyDown += SearchBox_KeyDown;

        this.GetObservable(IsVisibleProperty).SubscribeAction(@is =>
        {
            if (@is)
            {
                resultsList.SelectedItem = null;
                DispatcherTimer.RunOnce(() =>
                {
                    searchBox?.Focus();
                }, TimeSpan.FromMilliseconds(1));
            }
        });
    }
    
    private TextBox searchBox = null!;
    private ListBox resultsList = null!;
    private SelectingItemsControlSelectionAdapter adapter = null!;
    private bool searchBoxMoveToEnd = false;

    private void ResultCommit(object? sender, RoutedEventArgs e)
    {
        if (DataContext is QuestPickerViewModel vm)
        {
            var item = adapter.SelectedItem as QuestListItemViewModel;
            if (item == null)
            {
                if (vm.Quests.Count == 1)
                    item = vm.Quests[0];
            }

            if (item != null)
            {
                vm.CloseOkCommand.Execute(item.Entry);
            }
        }
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down || e.Key == Key.Up)
        {
            adapter.HandleKeyDown(e);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            ResultCommit(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (DataContext is QuestPickerViewModel vm)
                vm.CloseCancelCommand.Execute();
            e.Handled = true;
        }
    }
}