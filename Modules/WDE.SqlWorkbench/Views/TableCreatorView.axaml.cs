using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services.MessageBox;
using WDE.SqlWorkbench.Models.DataTypes;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Views;

public partial class TableCreatorView : UserControl
{
    public TableCreatorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void CompletionComboBox_OnOnEnterPressed(object? sender, CompletionComboBox.EnterPressedArgs e)
    {
        if (e.SelectedItem == null && !string.IsNullOrEmpty(e.SearchText))
        {
            if (MySqlType.TryParse(e.SearchText, out var type))
            {
                (sender as CompletionComboBox)!.SelectedItem = new DataTypeViewModel(type);
                e.Handled = true;
            }
            else
            {
                ViewBind.ResolveViewModel<IMessageBoxService>().SimpleDialog("Error", "Cannot parse type",
                        $"'{e.SearchText}' looks like an invalid MySQL type. If you think this is wrong, please report a bug.");
            }
        }
    }
}