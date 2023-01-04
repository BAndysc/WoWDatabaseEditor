using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Components;
using WDE.Parameters.ViewModels;

namespace WDE.CommonViews.Avalonia.Parameters.Views;

public partial class MultipleParametersPickerView : UserControl
{
    public MultipleParametersPickerView()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Grid_OnKeyDown(object? sender, KeyEventArgs e)
    {
        var grid = this.GetControl<GridView>("GridView");
        if (grid.SelectedItem is not MultipleParametersPickerItemViewModel item ||
            DataContext is not MultipleParametersPickerViewModel vm)
            return;
        
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            var selectedIndex = vm.PickedElements.IndexOf(item);
            
            vm.DeleteItemCommand.Execute(item);
            
            selectedIndex = Math.Min(selectedIndex, vm.PickedElements.Count - 1);
            if (selectedIndex >= 0)
                grid.SelectedItem = vm.PickedElements[selectedIndex];
        }
    }

    private void GridView_OnTextInput(object? sender, TextInputEventArgs e)
    {
        var grid = this.GetControl<GridView>("GridView");
        if (grid.SelectedItem is not MultipleParametersPickerItemViewModel item ||
            DataContext is not MultipleParametersPickerViewModel vm)
            return;

        if (e.Text == null || e.Text.Length != 1 || !char.IsDigit(e.Text[0]))
            return;

        if (grid.ListBoxImpl == null)
            return;

        var selectedBox = grid.ListBoxImpl.ContainerFromIndex(grid.ListBoxImpl.SelectedIndex);

        if (selectedBox == null)
            return;

        var editableTextBlock = selectedBox.FindDescendantOfType<EditableTextBlock>();
        editableTextBlock?.BeginEditing(e.Text);
    }
}