using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Services.TabularDataPickerService;

public partial class TabularDataPickerView : UserControl
{
    public TabularDataPickerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // quality of life feature: arrow down in searchbox focuses first element
    private void SearchBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            VirtualizedGridView gridView = this.FindControl<VirtualizedGridView>("GridView");
            if (gridView == null)
                return;

            if (gridView.SelectedIndex == null || gridView.SelectedIndex == -1)
                gridView.SelectedIndex = 0;
            gridView.Focus();

            e.Handled = true;
        }
    }
}