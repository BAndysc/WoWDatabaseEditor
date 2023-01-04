using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Utils;
using WDE.LootEditor.Picker.ViewModels;

namespace WDE.LootEditor.Picker.Views;

public partial class LootPickerView : UserControl
{
    public LootPickerView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            e.Handled = true;
            var veryFastTableView = this.FindControl<VeryFastTableView>("VeryFastTableView");
            if (veryFastTableView== null)
                return;
            veryFastTableView.Focus();
            if (!veryFastTableView.IsSelectedRowValid)
            {
                veryFastTableView.MultiSelection.Clear();
                if (veryFastTableView.Items!.Count > 0)
                {
                    veryFastTableView.SelectedRowIndex = new VerticalCursor(0, 0);
                    veryFastTableView.MultiSelection.Add(veryFastTableView.SelectedRowIndex);
                }
            }
        }
    }

    private void VeryFastTableView_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is LootPickerViewModel vm)
        {
            if (vm.FocusedGroup != null)
            {
                vm.Accept.Execute(null);
            }
        }
    }
}