using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.LootEditor.Configuration.Views;

public partial class LootEditorConfigurationView : UserControl
{
    public LootEditorConfigurationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void VeryFastTableView_OnValueUpdateRequest(string newValue)
    {
        if (DataContext is LootEditorConfiguration config &&
            config.SelectedButton != null)
        {
            foreach (var selected in config.SelectedButton.MultiSelection.All())
            {
                var cell = config.SelectedButton.Rows[selected.RowIndex].CellsList[config.SelectedButton.FocusedCellIndex];
                cell.UpdateFromString(newValue);
            }
        }
    }
}